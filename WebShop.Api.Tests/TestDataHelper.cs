namespace WebShop.Api.Tests;
// Namespace-level helper method for safe test database cleanup
public class TestDataHelper
{
    public static void SafeDeleteTestDatabase(string dbPath)
    {
        if (!File.Exists(dbPath)) return;
        
        // Force garbage collection to release any lingering file handles
        GC.Collect();
        GC.WaitForPendingFinalizers();
        
        // Retry loop for file deletion (SQLite might hold lock briefly)
        for (int i = 0; i < 5; i++)
        {
            try
            {
                File.Delete(dbPath);
                return;
            }
            catch (IOException)
            {
                if (i < 4) // Not the last retry
                {
                    System.Threading.Thread.Sleep(100); // Wait 100ms before retry
                }
                else
                {
                    // Last retry failed - suppress the exception to avoid test failures
                    // The temp file will eventually be cleaned up by the OS
                }
            }
        }
    }
}