namespace ZyGames.Framework.Services.Lifecycle
{
    public static class Lifecycles
    {
        public static class Stage
        {
            internal const int Core = 1000;

            internal const int System = 2000;

            public const int User = 3000;
        }

        public static class State
        {
            public static class ServiceHost
            {
                public const int Starting = 1;

                public const int Started = 2;

                public const int Stopped = 3;
            }

            internal static class ActivationDirectory
            {
                public const int Changed = 1;
            }

            public static class Membership
            {
                public const int Changed = 1;
            }
        }
    }
}
