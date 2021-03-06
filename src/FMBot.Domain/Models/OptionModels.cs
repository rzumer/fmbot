using IF.Lastfm.Core.Api.Enums;

namespace FMBot.Domain.Models
{
    public class TimeSettingsModel
    {
        public string UrlParameter { get; set; }
        public string ApiParameter { get; set; }
        public string Description { get; set; }
        public LastStatsTimeSpan LastStatsTimeSpan { get; set; }
        public ChartTimePeriod ChartTimePeriod { get; set; }
        public bool UsePlays { get; set; }
        public int? PlayDays { get; set; }
    }

    public class UserSettingsModel
    {
        public string UserNameLastFm { get; set; }
        public bool DifferentUser { get; set; }
        public ulong? DiscordUserId { get; set; }
    }
}
