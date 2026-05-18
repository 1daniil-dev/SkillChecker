using System.Buffers.Binary;
using System.Text;

namespace SkillChecker.Common.Protocol
{
    public static class ProtocolFramer
    {
        public const int HeaderSize = 4;
        public const int MaxFrameSize = 1024 * 1024;

        public static void WriteFrame(Stream stream, string payload)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            string safe = payload == null ? "" : payload;
            byte[] body = Encoding.UTF8.GetBytes(safe);
            if (body.Length > MaxFrameSize)
            {
                throw new ProtocolException(
                    "Размер сообщения " + body.Length + " байт превышает лимит " + MaxFrameSize);
            }
            byte[] header = new byte[HeaderSize];
            BinaryPrimitives.WriteInt32BigEndian(header, body.Length);
            stream.Write(header, 0, header.Length);
            if (body.Length > 0)
            {
                stream.Write(body, 0, body.Length);
            }
            stream.Flush();
        }

        public static string ReadFrame(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            byte[] header = new byte[HeaderSize];
            ReadExactly(stream, header, 0, HeaderSize);
            int length = BinaryPrimitives.ReadInt32BigEndian(header);
            if (length < 0)
            {
                throw new ProtocolException("Отрицательная длина фрейма: " + length);
            }
            if (length > MaxFrameSize)
            {
                throw new ProtocolException(
                    "Длина фрейма " + length + " превышает лимит " + MaxFrameSize);
            }
            if (length == 0)
            {
                return "";
            }
            byte[] body = new byte[length];
            ReadExactly(stream, body, 0, length);
            return Encoding.UTF8.GetString(body);
        }

        private static void ReadExactly(Stream stream, byte[] buffer, int offset, int count)
        {
            int read = 0;
            while (read < count)
            {
                int n = stream.Read(buffer, offset + read, count - read);
                if (n <= 0)
                {
                    throw new EndOfStreamException(
                        "Соединение закрыто до полного чтения " + count + " байт (прочитано " + read + ")");
                }
                read += n;
            }
        }
    }

    public class ProtocolException : Exception
    {
        public ProtocolException(string message) : base(message)
        {
        }
    }
}
