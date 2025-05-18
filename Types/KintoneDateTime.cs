namespace KintoneNetLibrary.Types;

public class KintoneDateTime
{
    public enum DateTimeType {
        /// <summary>
        /// 日時型
        /// </summary>
        DateTime,
        /// <summary>
        /// 時刻型
        /// </summary>
        Time,
        /// <summary>
        /// 日付型
        /// </summary>
        Ymd,
    }

    public DateTime Value { get; set; }

    public KintoneDateTime() {
        this.Value = DateTime.MinValue;
    }

    public KintoneDateTime(DateTime value) {
        this.Value = value;
    }

    public override string ToString() {
        return this.Value.ToString("yyyy-MM-ddTHH:mm:ssZ");
    }
}
