namespace ZyGames.Framework.Services.Runtime
{
    public interface IReferenceRuntime
    {
        void InvokeMethod(Reference reference, int methodId, object[] arguments, InvokeMethodOptions options, int timeoutMills);

        T InvokeMethod<T>(Reference reference, int methodId, object[] arguments, InvokeMethodOptions options, int timeoutMills);
    }
}
