namespace Auger.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class MoreCleanup : DbMigration
    {
        public override void Up()
        {
            DropIndex("dbo.Enrollments", new[] { "LtiUserId" });
            AddColumn("dbo.Scripts", "ScriptText", c => c.String());
            AlterColumn("dbo.Assignments", "AssignmentName", c => c.String(maxLength: 255));
            AlterColumn("dbo.Assignments", "DueDate", c => c.DateTime());
            AlterColumn("dbo.Scripts", "ScriptName", c => c.String(maxLength: 255));
            AlterColumn("dbo.Scripts", "DeviceId", c => c.String(maxLength: 50));
            AlterColumn("dbo.Pages", "PageName", c => c.String(maxLength: 255));
            AlterColumn("dbo.Courses", "CourseTitle", c => c.String(maxLength: 255));
            AlterColumn("dbo.Courses", "CourseLabel", c => c.String(maxLength: 255));
            AlterColumn("dbo.Enrollments", "UserName", c => c.String(maxLength: 128));
            AlterColumn("dbo.Enrollments", "AllRoles", c => c.String(maxLength: 128));
            AlterColumn("dbo.StudentSubmissions", "CommitId", c => c.String(maxLength: 128));
            AlterColumn("dbo.StudentSubmissions", "SubmissionName", c => c.String(maxLength: 255));
            AlterColumn("dbo.LtiConsumers", "Name", c => c.String(nullable: false, maxLength: 255));
            AlterColumn("dbo.LtiConsumers", "Key", c => c.String(nullable: false, maxLength: 128));
            AlterColumn("dbo.LtiConsumers", "Secret", c => c.String(nullable: false, maxLength: 128));
            AlterColumn("dbo.LtiOutcomes", "ContextTitle", c => c.String(maxLength: 255));
            AlterColumn("dbo.LtiOutcomes", "LisResultSourcedId", c => c.String(maxLength: 255));
            AlterColumn("dbo.LtiOutcomes", "ServiceUrl", c => c.String(maxLength: 255));
            DropColumn("dbo.Enrollments", "LtiUserId");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Enrollments", "LtiUserId", c => c.String(maxLength: 50));
            AlterColumn("dbo.LtiOutcomes", "ServiceUrl", c => c.String());
            AlterColumn("dbo.LtiOutcomes", "LisResultSourcedId", c => c.String());
            AlterColumn("dbo.LtiOutcomes", "ContextTitle", c => c.String());
            AlterColumn("dbo.LtiConsumers", "Secret", c => c.String(nullable: false));
            AlterColumn("dbo.LtiConsumers", "Key", c => c.String(nullable: false));
            AlterColumn("dbo.LtiConsumers", "Name", c => c.String(nullable: false));
            AlterColumn("dbo.StudentSubmissions", "SubmissionName", c => c.String());
            AlterColumn("dbo.StudentSubmissions", "CommitId", c => c.String());
            AlterColumn("dbo.Enrollments", "AllRoles", c => c.String());
            AlterColumn("dbo.Enrollments", "UserName", c => c.String());
            AlterColumn("dbo.Courses", "CourseLabel", c => c.String());
            AlterColumn("dbo.Courses", "CourseTitle", c => c.String());
            AlterColumn("dbo.Pages", "PageName", c => c.String());
            AlterColumn("dbo.Scripts", "DeviceId", c => c.String());
            AlterColumn("dbo.Scripts", "ScriptName", c => c.String());
            AlterColumn("dbo.Assignments", "DueDate", c => c.DateTime(nullable: false));
            AlterColumn("dbo.Assignments", "AssignmentName", c => c.String());
            DropColumn("dbo.Scripts", "ScriptText");
            CreateIndex("dbo.Enrollments", "LtiUserId");
        }
    }
}
