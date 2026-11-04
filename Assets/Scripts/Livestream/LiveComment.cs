namespace StreamerGame.Livestream
{
    public enum CommentType
    {
        FanNormal,
        FanExcited,
        Question,
        Troll,
        DonationStar,
        DonationSuperchat,
        DonationRocket
    }

    [System.Serializable]
    public class LiveComment
    {
        public string username;
        public string message;
        public CommentType type;
        public long donationVND;
        public string badge;

        public LiveComment(string username, string message, CommentType type, long donationVND = 0, string badge = "")
        {
            this.username = username;
            this.message = message;
            this.type = type;
            this.donationVND = donationVND;
            this.badge = badge;
        }
    }
}
