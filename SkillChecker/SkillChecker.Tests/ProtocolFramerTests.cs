using System.IO;
using System.Text;
using SkillChecker.Common.Protocol;

namespace SkillChecker.Tests
{
    public class ProtocolFramerTests
    {
        // Пустой payload: пишется только 4-байтовый заголовок со значением 0, тела нет
        [Fact]
        public void WriteFrame_Empty_WritesZeroLengthHeader()
        {
            using MemoryStream ms = new MemoryStream();
            ProtocolFramer.WriteFrame(ms, "");
            byte[] data = ms.ToArray();
            Assert.Equal(4, data.Length);
            Assert.Equal(0, data[0]);
            Assert.Equal(0, data[1]);
            Assert.Equal(0, data[2]);
            Assert.Equal(0, data[3]);
        }

        // Заголовок длины пишется в сетевом порядке байт (big-endian), payload — UTF-8 байты
        [Fact]
        public void WriteFrame_Ascii_HeaderIsBigEndianLength()
        {
            using MemoryStream ms = new MemoryStream();
            ProtocolFramer.WriteFrame(ms, "OK");
            byte[] data = ms.ToArray();
            Assert.Equal(6, data.Length);
            Assert.Equal(0, data[0]);
            Assert.Equal(0, data[1]);
            Assert.Equal(0, data[2]);
            Assert.Equal(2, data[3]);
            Assert.Equal((byte)'O', data[4]);
            Assert.Equal((byte)'K', data[5]);
        }

        // Round-trip: то что записали через WriteFrame равно тому что прочитали ReadFrame
        [Fact]
        public void RoundTrip_Ascii_Equal()
        {
            using MemoryStream ms = new MemoryStream();
            ProtocolFramer.WriteFrame(ms, "GET_TESTS");
            ms.Position = 0;
            string result = ProtocolFramer.ReadFrame(ms);
            Assert.Equal("GET_TESTS", result);
        }

        // Round-trip с кириллицей и многобайтовым UTF-8 не теряет данные
        [Fact]
        public void RoundTrip_Unicode_Equal()
        {
            string payload = "Тест|Имя|Группа АБ-21";
            using MemoryStream ms = new MemoryStream();
            ProtocolFramer.WriteFrame(ms, payload);
            ms.Position = 0;
            string result = ProtocolFramer.ReadFrame(ms);
            Assert.Equal(payload, result);
        }

        // Перевод строки внутри payload больше не ломает протокол (раньше \n был терминатором)
        [Fact]
        public void RoundTrip_PayloadWithNewlines_Preserved()
        {
            string payload = "line1\nline2\rline3\r\nline4";
            using MemoryStream ms = new MemoryStream();
            ProtocolFramer.WriteFrame(ms, payload);
            ms.Position = 0;
            string result = ProtocolFramer.ReadFrame(ms);
            Assert.Equal(payload, result);
        }

        // Разделитель | внутри полей сохраняется без экранирования — длина уже задана заголовком
        [Fact]
        public void RoundTrip_PayloadWithPipe_Preserved()
        {
            string payload = "GET_TEST|MyTest|10";
            using MemoryStream ms = new MemoryStream();
            ProtocolFramer.WriteFrame(ms, payload);
            ms.Position = 0;
            string result = ProtocolFramer.ReadFrame(ms);
            Assert.Equal(payload, result);
        }

        // Два фрейма подряд читаются как два независимых сообщения, границы не путаются
        [Fact]
        public void RoundTrip_TwoFramesBackToBack_ReadIndependently()
        {
            using MemoryStream ms = new MemoryStream();
            ProtocolFramer.WriteFrame(ms, "FIRST");
            ProtocolFramer.WriteFrame(ms, "SECOND");
            ms.Position = 0;
            Assert.Equal("FIRST", ProtocolFramer.ReadFrame(ms));
            Assert.Equal("SECOND", ProtocolFramer.ReadFrame(ms));
        }

        // null трактуется как пустая строка — никаких NullReferenceException
        [Fact]
        public void WriteFrame_Null_TreatedAsEmpty()
        {
            using MemoryStream ms = new MemoryStream();
            ProtocolFramer.WriteFrame(ms, null!);
            byte[] data = ms.ToArray();
            Assert.Equal(4, data.Length);
            ms.Position = 0;
            string result = ProtocolFramer.ReadFrame(ms);
            Assert.Equal("", result);
        }

        // Защита от негодяя со стороны клиента: попытка отправить payload > MaxFrameSize — отказ
        [Fact]
        public void WriteFrame_PayloadAboveMax_Throws()
        {
            using MemoryStream ms = new MemoryStream();
            string huge = new string('A', ProtocolFramer.MaxFrameSize + 1);
            Assert.Throws<ProtocolException>(() => ProtocolFramer.WriteFrame(ms, huge));
        }

        // DoS-защита: если клиент заявит длину больше лимита, сервер кинет исключение
        // ДО того как начнёт читать тело — память не выделяется
        [Fact]
        public void ReadFrame_DeclaredLengthAboveMax_ThrowsBeforeReadingBody()
        {
            using MemoryStream ms = new MemoryStream();
            int evilLength = ProtocolFramer.MaxFrameSize + 1;
            ms.WriteByte((byte)((evilLength >> 24) & 0xFF));
            ms.WriteByte((byte)((evilLength >> 16) & 0xFF));
            ms.WriteByte((byte)((evilLength >> 8) & 0xFF));
            ms.WriteByte((byte)(evilLength & 0xFF));
            ms.Position = 0;
            Assert.Throws<ProtocolException>(() => ProtocolFramer.ReadFrame(ms));
        }

        // Отрицательная длина (старший бит установлен) — мусор, отказ
        [Fact]
        public void ReadFrame_NegativeDeclaredLength_Throws()
        {
            using MemoryStream ms = new MemoryStream();
            ms.WriteByte(0xFF);
            ms.WriteByte(0xFF);
            ms.WriteByte(0xFF);
            ms.WriteByte(0xFF);
            ms.Position = 0;
            Assert.Throws<ProtocolException>(() => ProtocolFramer.ReadFrame(ms));
        }

        // Клиент отвалился посреди отправки заголовка — корректный EndOfStreamException
        [Fact]
        public void ReadFrame_HeaderTruncated_Throws()
        {
            using MemoryStream ms = new MemoryStream(new byte[] { 0x00, 0x00 });
            Assert.Throws<EndOfStreamException>(() => ProtocolFramer.ReadFrame(ms));
        }

        // Заголовок пришёл целиком, а тело оборвалось — тоже EndOfStreamException
        [Fact]
        public void ReadFrame_BodyTruncated_Throws()
        {
            using MemoryStream ms = new MemoryStream();
            ms.WriteByte(0x00);
            ms.WriteByte(0x00);
            ms.WriteByte(0x00);
            ms.WriteByte(0x10);
            ms.Write(Encoding.UTF8.GetBytes("only5"), 0, 5);
            ms.Position = 0;
            Assert.Throws<EndOfStreamException>(() => ProtocolFramer.ReadFrame(ms));
        }

        // Подключение закрыто без единого байта — EndOfStreamException
        [Fact]
        public void ReadFrame_EmptyStream_Throws()
        {
            using MemoryStream ms = new MemoryStream();
            Assert.Throws<EndOfStreamException>(() => ProtocolFramer.ReadFrame(ms));
        }

        // Граничный случай: payload ровно MaxFrameSize — допустим, должен пройти round-trip
        [Fact]
        public void RoundTrip_ExactlyMaxSize_OK()
        {
            using MemoryStream ms = new MemoryStream();
            string maxPayload = new string('X', ProtocolFramer.MaxFrameSize);
            ProtocolFramer.WriteFrame(ms, maxPayload);
            ms.Position = 0;
            string result = ProtocolFramer.ReadFrame(ms);
            Assert.Equal(maxPayload, result);
        }
    }
}
