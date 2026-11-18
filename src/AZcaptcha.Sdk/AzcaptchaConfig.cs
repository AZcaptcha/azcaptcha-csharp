using System;
using System.Threading;
using System.Threading.Tasks;

namespace Azcaptcha
{
    /// <summary>Overrides for the client; the defaults are production-ready.</summary>
    public sealed class AzcaptchaConfig
    {
        public string Host { get; set; } = "https://azcaptcha.com";

        public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromSeconds(30);

        public int SoftId { get; set; }

        public ITransport? Transport { get; set; }

        public Func<TimeSpan, CancellationToken, Task> Delay { get; set; } =
            (duration, token) => Task.Delay(duration, token);

        public AzcaptchaConfig WithHost(string host)
        {
            Host = host;
            return this;
        }

        public AzcaptchaConfig WithTransport(ITransport transport)
        {
            Transport = transport;
            return this;
        }

        public AzcaptchaConfig WithDelay(Func<TimeSpan, CancellationToken, Task> delay)
        {
            Delay = delay;
            return this;
        }
    }
}
