// Copyright (c) Hitcents
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Unity.IO.Compression {
    /// <summary>
    /// NOTE: this is a hacked in replacement for the SR class
    ///     Unity games don't care about localized exception messages, so we just hacked these in the best we could
    /// </summary>
    internal class SR
    {
        public const string ArgumentOutOfRange_Enum = "Argument out of range";
        public const string CorruptedGZipHeader = "Corrupted gzip header";
        public const string CannotReadFromDeflateStream = "Cannot read from deflate stream";
        public const string CannotWriteToDeflateStream = "Cannot write to deflate stream";
        public const string GenericInvalidData = "Invalid data";
        public const string InvalidCRC = "Invalid CRC";
        public const string InvalidStreamSize = "Invalid stream size";
        public const string InvalidHuffmanData = "Invalid Huffman data";
        public const string InvalidBeginCall = "Invalid begin call";
        public const string InvalidEndCall = "Invalid end call";
        public const string InvalidBlockLength = "Invalid block length";
        public const string InvalidArgumentOffsetCount = "Invalid argument offset count";
        public const string NotSupported = "Not supported";
        public const string NotWriteableStream = "Not a writeable stream";
        public const string NotReadableStream = "Not a readable stream";
        public const string ObjectDisposed_StreamClosed = "Object disposed";
        public const string UnknownState = "Unknown state";
        public const string UnknownCompressionMode = "Unknown compression mode";
        public const string UnknownBlockType = "Unknown block type";
        public const string EntriesInCreateMode = "EntriesInCreateMode";
        public const string EntryNameEncodingNotSupported = "EntryNameEncodingNotSupported";
        public const string CannotBeEmpty = "CannotBeEmpty";
        public const string CreateInReadMode = "CreateInReadMode";
        public const string CreateModeCreateEntryWhileOpen = "CreateModeCreateEntryWhileOpen";
        public const string CreateModeCapabilities = "CreateModeCapabilities";
        public const string ReadModeCapabilities = "ReadModeCapabilities";
        public const string UpdateModeCapabilities = "UpdateModeCapabilities";
        public const string UnexpectedEndOfStream = "UnexpectedEndOfStream";
        public const string FieldTooBigUncompressedSize = "FieldTooBigUncompressedSize";
        public const string FieldTooBigCompressedSize = "FieldTooBigCompressedSize";
        public const string FieldTooBigLocalHeaderOffset = "FieldTooBigLocalHeaderOffset";
        public const string FieldTooBigStartDiskNumber = "FieldTooBigStartDiskNumber";
        public const string HiddenStreamName = "HiddenStreamName";
        public const string WritingNotSupported = "WritingNotSupported";
        public const string SeekingNotSupported = "SeekingNotSupported";
        public const string ReadingNotSupported = "ReadingNotSupported";
        public const string SetLengthRequiresSeekingAndWriting = "SetLengthRequiresSeekingAndWriting";
        public const string ArgumentNeedNonNegative = "ArgumentNeedNonNegative";
        public const string OffsetLengthInvalid = "OffsetLengthInvalid";
        public const string NumEntriesWrong = "NumEntriesWrong";
        public const string CentralDirectoryInvalid = "CentralDirectoryInvalid";
        public const string EOCDNotFound = "EOCDNotFound";
        public const string SplitSpanned = "SplitSpanned";
        public const string FieldTooBigOffsetToZip64EOCD = "FieldTooBigOffsetToZip64EOCD";
        public const string Zip64EOCDNotWhereExpected = "Zip64EOCDNotWhereExpected";
        public const string FieldTooBigNumEntries = "FieldTooBigNumEntries";
        public const string FieldTooBigOffsetToCD = "FieldTooBigOffsetToCD";
        public const string CDCorrupt = "CDCorrupt";
        public const string EntryNamesTooLong = "EntryNamesTooLong";
        public const string LengthAfterWrite = "LengthAfterWrite";
        public const string ReadOnlyArchive = "ReadOnlyArchive";
        public const string FrozenAfterWrite = "FrozenAfterWrite";
        public const string DateTimeOutOfRange = "DateTimeOutOfRange";
        public const string DeleteOpenEntry = "DeleteOpenEntry";
        public const string DeleteOnlyInUpdate = "DeleteOnlyInUpdate";
        public const string LocalFileHeaderCorrupt = "LocalFileHeaderCorrupt";
        public const string CreateModeWriteOnceAndOneEntryAtATime = "CreateModeWriteOnceAndOneEntryAtATime";
        public const string UpdateModeOneStream = "UpdateModeOneStream";
        public const string UnsupportedCompression = "UnsupportedCompression";
        public const string EntryTooLarge = "EntryTooLarge";
        public const string DeletedEntry = "DeletedEntry";

        private SR()
        {
        }

        internal static string GetString(string p)
        {
            //HACK: just return the string passed in, not doing localization
            return p;
        }

        internal static string Format(string p, System.IO.EndOfStreamException ex)
        {
            return p;
        }
    }
}
