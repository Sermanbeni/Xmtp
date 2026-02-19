namespace Xmtp
{
    public class XmtpMessageResponse<TResponse>
    {
        public XmtpResultCode ResultCode { get; set; }
        public TResponse? Value { get; set; }

        public XmtpMessageResponse(XmtpResultCode resultCode, TResponse? value)
        {
            ResultCode = resultCode;
            Value = value;
        }
    }

    public enum XmtpResultCode
    {
        Success,
        Timeout,
        ParseFailure,
        Blocked
    }
}
