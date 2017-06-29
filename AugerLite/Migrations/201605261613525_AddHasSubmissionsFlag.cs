namespace Auger.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddHasSubmissionsFlag : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.StudentAssignments", "HasSubmission", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.StudentAssignments", "HasSubmission");
        }
    }
}
