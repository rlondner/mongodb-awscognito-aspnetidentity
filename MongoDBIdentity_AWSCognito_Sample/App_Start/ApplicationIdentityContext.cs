namespace IdentitySample
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using AspNet.Identity.MongoDB;
    using Models;
    using MongoDB.Driver;
    using System.Configuration;

    public class ApplicationIdentityContext : IDisposable
	{
		public static ApplicationIdentityContext Create()
		{
            string strConnectionString = ConfigurationManager.AppSettings["ConnectionString"];
            //strConnectionString = "mongodb://localhost:27017";
            string strDatabaseName = ConfigurationManager.AppSettings["DatabaseName"];
            //strDatabaseName = "AspNet_Identity_MongoDB";
            var client = new MongoClient(strConnectionString);
			var database = client.GetDatabase(strDatabaseName);
			var users = database.GetCollection<ApplicationUser>("users");
			var roles = database.GetCollection<IdentityRole>("roles");
			return new ApplicationIdentityContext(users, roles);
		}

		private ApplicationIdentityContext(IMongoCollection<ApplicationUser> users, IMongoCollection<IdentityRole> roles)
		{
			Users = users;
			Roles = roles;
		}

		public IMongoCollection<IdentityRole> Roles { get; set; }

		public IMongoCollection<ApplicationUser> Users { get; set; }

		public Task<List<IdentityRole>> AllRolesAsync()
		{
			return Roles.Find(r => true).ToListAsync();
		}

		public void Dispose()
		{
		}
	}
}