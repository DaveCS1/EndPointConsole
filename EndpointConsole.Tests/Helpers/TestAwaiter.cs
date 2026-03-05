namespace EndpointConsole.Tests.Helpers;

internal static class TestAwaiter
{
    public static async Task WaitUntilAsync(Func<bool> predicate, TimeSpan timeout)
    {
        var start = DateTime.UtcNow;
        while (!predicate())
        {
            if (DateTime.UtcNow - start > timeout)
            {
                throw new TimeoutException("Condition was not met before timeout.");
            }

            await Task.Delay(25);
        }
    }
}
