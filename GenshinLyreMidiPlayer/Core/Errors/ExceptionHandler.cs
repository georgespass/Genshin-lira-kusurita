using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Melanchall.DryWetMidi.Core;
using ModernWpf.Controls;

namespace GenshinLyreMidiPlayer.Core.Errors
{
    internal static class ExceptionHandler
    {
        private static readonly Dictionary<Type, List<Enum>> ExceptionOptions = new()
        {
            [typeof(InvalidChannelEventParameterValueException)] = new List<Enum>
            {
                InvalidChannelEventParameterValuePolicy.SnapToLimits,
                InvalidChannelEventParameterValuePolicy.ReadValid
            },
            [typeof(InvalidMetaEventParameterValueException)] = new List<Enum>
            {
                InvalidMetaEventParameterValuePolicy.SnapToLimits
            },
            [typeof(InvalidSystemCommonEventParameterValueException)] = new List<Enum>
            {
                InvalidSystemCommonEventParameterValuePolicy.SnapToLimits
            },
            [typeof(UnknownChunkException)] = new List<Enum>
            {
                UnknownChunkIdPolicy.ReadAsUnknownChunk,
                UnknownChunkIdPolicy.Skip
            },

            [typeof(InvalidChunkSizeException)] = new List<Enum>
            {
                InvalidChunkSizePolicy.Ignore
            },
            [typeof(MissedEndOfTrackEventException)] = new List<Enum>
            {
                MissedEndOfTrackPolicy.Ignore
            },
            [typeof(NoHeaderChunkException)] = new List<Enum>
            {
                NoHeaderChunkPolicy.Ignore
            },
            [typeof(NotEnoughBytesException)] = new List<Enum>
            {
                NotEnoughBytesPolicy.Ignore
            },
            [typeof(UnexpectedTrackChunksCountException)] = new List<Enum>
            {
                UnexpectedTrackChunksCountPolicy.Ignore
            },
            [typeof(UnknownChannelEventException)] = new List<Enum>
            {
                UnknownChannelEventPolicy.SkipStatusByte
            },
            [typeof(UnknownFileFormatException)] = new List<Enum>
            {
                UnknownFileFormatPolicy.Ignore
            }
        };

        private static readonly IReadOnlyList<Type> FatalExceptions = new List<Type>
        {
            typeof(InvalidMidiTimeCodeComponentException),
            typeof(TooManyTrackChunksException),
            typeof(UnexpectedRunningStatusException)
        };

        /// <summary>
        ///     Tries to handle a <see cref="MidiException" />
        ///     with user interaction whether to abort the reading or ignore the exception.
        /// </summary>
        /// <param name="e">The <see cref="MidiException" /> that was thrown.</param>
        /// <param name="settings">The existing <see cref="ReadingSettings" /> to modify.</param>
        /// <returns>A <see cref="bool" /> whether reading can continue or not.</returns>
        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        [SuppressMessage("ReSharper", "CheckForReferenceEqualityInstead.1")]
        public static async Task<bool> TryHandleException(Exception e, ReadingSettings? settings = null)
        {
            var command = ExceptionOptions
                .FirstOrDefault(type =>
                    type.Key.Equals(e.GetType())).Value;

            var exceptionDialog = new ErrorContentDialog(e, command);
            var result = await exceptionDialog.ShowAsync();

            if (result == ContentDialogResult.None || FatalExceptions.Contains(e.GetType()))
                return false;

            var option = result switch
            {
                ContentDialogResult.Primary   => command?.ElementAtOrDefault(0),
                ContentDialogResult.Secondary => command?.ElementAtOrDefault(1)
            };

            settings ??= new ReadingSettings();
            switch (e)
            {
                // User selectable policy
                case InvalidChannelEventParameterValueException:
                    settings.InvalidChannelEventParameterValuePolicy =
                        (InvalidChannelEventParameterValuePolicy) option;
                    break;
                case InvalidMetaEventParameterValueException:
                    settings.InvalidMetaEventParameterValuePolicy =
                        (InvalidMetaEventParameterValuePolicy) option;
                    break;
                case InvalidSystemCommonEventParameterValueException:
                    settings.InvalidSystemCommonEventParameterValuePolicy =
                        (InvalidSystemCommonEventParameterValuePolicy) option;
                    break;
                case UnknownChannelEventException:
                    settings.UnknownChannelEventPolicy =
                        (UnknownChannelEventPolicy) option;
                    break;
                case UnknownChunkException:
                    settings.UnknownChunkIdPolicy =
                        (UnknownChunkIdPolicy) option;
                    break;

                // Ignorable policies
                case InvalidChunkSizeException:
                    settings.InvalidChunkSizePolicy = InvalidChunkSizePolicy.Ignore;
                    break;
                case MissedEndOfTrackEventException:
                    settings.MissedEndOfTrackPolicy = MissedEndOfTrackPolicy.Ignore;
                    break;
                case NoHeaderChunkException:
                    settings.NoHeaderChunkPolicy = NoHeaderChunkPolicy.Ignore;
                    break;
                case NotEnoughBytesException:
                    settings.NotEnoughBytesPolicy = NotEnoughBytesPolicy.Ignore;
                    break;
                case UnexpectedTrackChunksCountException:
                    settings.UnexpectedTrackChunksCountPolicy = UnexpectedTrackChunksCountPolicy.Ignore;
                    break;

                case UnknownFileFormatException:
                    settings.UnknownFileFormatPolicy = UnknownFileFormatPolicy.Ignore;
                    break;

                // Fatal exceptions
                case InvalidMidiTimeCodeComponentException:
                case TooManyTrackChunksException:
                case UnexpectedRunningStatusException:
                    return false;
            }

            return true;
        }
    }
}