using ContactPro.Data;
using Microsoft.EntityFrameworkCore;
using MimeKit.Cryptography;

namespace ContactPro.Helpers
{
    public static class DataHelper
    {
        public static async Task ManageDataAsync(IServiceProvider svcProvider)
        {
            var dbContextService = svcProvider.GetRequiredService<ApplicationDbContext>();

            await dbContextService.Database.MigrateAsync();
        }
    }
}
