namespace Auger.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RepositoryTypos : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.StudentSubmissions", "CommitId", c => c.String());
            AddColumn("dbo.StudentSubmissions", "FullResultsJson", c => c.String());
            DropColumn("dbo.StudentSubmissions", "CommitTime");
            DropColumn("dbo.StudentSubmissions", "FulResultsJson");
        }
        
        public override void Down()
        {
            AddColumn("dbo.StudentSubmissions", "FulResultsJson", c => c.String());
            AddColumn("dbo.StudentSubmissions", "CommitTime", c => c.String());
            DropColumn("dbo.StudentSubmissions", "FullResultsJson");
            DropColumn("dbo.StudentSubmissions", "CommitId");
        }
    }
}
