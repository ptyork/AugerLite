namespace Auger.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Assignments",
                c => new
                    {
                        AssignmentId = c.Int(nullable: false, identity: true),
                        CourseId = c.Int(nullable: false),
                        AssignmentName = c.String(),
                        DueDate = c.DateTime(nullable: false),
                        LtiResourceLinkId = c.String(maxLength: 50),
                        DateCreated = c.DateTime(nullable: false),
                        DateModified = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.AssignmentId)
                .ForeignKey("dbo.Courses", t => t.CourseId, cascadeDelete: true)
                .Index(t => t.CourseId)
                .Index(t => t.LtiResourceLinkId);
            
            CreateTable(
                "dbo.Scripts",
                c => new
                    {
                        ScriptId = c.Int(nullable: false, identity: true),
                        AssignmentId = c.Int(),
                        PageId = c.Int(),
                        ScriptName = c.String(),
                        IsPreGrade = c.Boolean(nullable: false),
                        DeviceId = c.String(),
                        DateCreated = c.DateTime(nullable: false),
                        DateModified = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.ScriptId)
                .ForeignKey("dbo.Assignments", t => t.AssignmentId)
                .ForeignKey("dbo.Pages", t => t.PageId)
                .Index(t => t.AssignmentId)
                .Index(t => t.PageId);
            
            CreateTable(
                "dbo.Pages",
                c => new
                    {
                        PageId = c.Int(nullable: false, identity: true),
                        AssignmentId = c.Int(nullable: false),
                        PageName = c.String(),
                        DateCreated = c.DateTime(nullable: false),
                        DateModified = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.PageId)
                .ForeignKey("dbo.Assignments", t => t.AssignmentId, cascadeDelete: true)
                .Index(t => t.AssignmentId);
            
            CreateTable(
                "dbo.Courses",
                c => new
                    {
                        CourseId = c.Int(nullable: false, identity: true),
                        CourseTitle = c.String(),
                        CourseLabel = c.String(),
                        LtiContextId = c.String(maxLength: 50),
                        IsActive = c.Boolean(nullable: false),
                        DateCreated = c.DateTime(nullable: false),
                        DateModified = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.CourseId)
                .Index(t => t.LtiContextId);
            
            CreateTable(
                "dbo.Enrollments",
                c => new
                    {
                        EnrollmentId = c.Int(nullable: false, identity: true),
                        CourseId = c.Int(nullable: false),
                        UserId = c.String(maxLength: 128),
                        UserName = c.String(),
                        AllRoles = c.String(),
                        LtiUserId = c.String(maxLength: 50),
                        IsActive = c.Boolean(nullable: false),
                        DateCreated = c.DateTime(nullable: false),
                        DateModified = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.EnrollmentId)
                .ForeignKey("dbo.Courses", t => t.CourseId, cascadeDelete: true)
                .ForeignKey("dbo.AspNetUsers", t => t.UserId)
                .Index(t => t.CourseId)
                .Index(t => t.UserId)
                .Index(t => t.LtiUserId);
            
            CreateTable(
                "dbo.StudentAssignments",
                c => new
                    {
                        StudentAssignmentId = c.Int(nullable: false, identity: true),
                        AssignmentId = c.Int(),
                        EnrollmentId = c.Int(nullable: false),
                        DateCreated = c.DateTime(nullable: false),
                        DateModified = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.StudentAssignmentId)
                .ForeignKey("dbo.Assignments", t => t.AssignmentId)
                .ForeignKey("dbo.Enrollments", t => t.EnrollmentId, cascadeDelete: true)
                .Index(t => t.AssignmentId)
                .Index(t => t.EnrollmentId);
            
            CreateTable(
                "dbo.StudentSubmissions",
                c => new
                    {
                        StudentSubmissionId = c.Int(nullable: false, identity: true),
                        StudentAssignmentId = c.Int(nullable: false),
                        CommitTime = c.String(),
                        SubmissionName = c.String(),
                        Succeeded = c.Boolean(nullable: false),
                        Exception = c.String(),
                        PreSubmissionResultsJson = c.String(),
                        FulResultsJson = c.String(),
                        DateCreated = c.DateTime(nullable: false),
                        DateModified = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.StudentSubmissionId)
                .ForeignKey("dbo.StudentAssignments", t => t.StudentAssignmentId, cascadeDelete: true)
                .Index(t => t.StudentAssignmentId);
            
            CreateTable(
                "dbo.AspNetUsers",
                c => new
                    {
                        Id = c.String(nullable: false, maxLength: 128),
                        FirstName = c.String(),
                        LastName = c.String(),
                        Email = c.String(maxLength: 256),
                        EmailConfirmed = c.Boolean(nullable: false),
                        PasswordHash = c.String(),
                        SecurityStamp = c.String(),
                        PhoneNumber = c.String(),
                        PhoneNumberConfirmed = c.Boolean(nullable: false),
                        TwoFactorEnabled = c.Boolean(nullable: false),
                        LockoutEndDateUtc = c.DateTime(),
                        LockoutEnabled = c.Boolean(nullable: false),
                        AccessFailedCount = c.Int(nullable: false),
                        UserName = c.String(nullable: false, maxLength: 256),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.UserName, unique: true, name: "UserNameIndex");
            
            CreateTable(
                "dbo.AspNetUserClaims",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserId = c.String(nullable: false, maxLength: 128),
                        ClaimType = c.String(),
                        ClaimValue = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.AspNetUserLogins",
                c => new
                    {
                        LoginProvider = c.String(nullable: false, maxLength: 128),
                        ProviderKey = c.String(nullable: false, maxLength: 128),
                        UserId = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => new { t.LoginProvider, t.ProviderKey, t.UserId })
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.AspNetUserRoles",
                c => new
                    {
                        UserId = c.String(nullable: false, maxLength: 128),
                        RoleId = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => new { t.UserId, t.RoleId })
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .ForeignKey("dbo.AspNetRoles", t => t.RoleId, cascadeDelete: true)
                .Index(t => t.UserId)
                .Index(t => t.RoleId);
            
            CreateTable(
                "dbo.LtiConsumers",
                c => new
                    {
                        LtiConsumerId = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false),
                        Key = c.String(nullable: false),
                        Secret = c.String(nullable: false),
                        DateCreated = c.DateTime(nullable: false),
                        DateModified = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.LtiConsumerId);
            
            CreateTable(
                "dbo.LtiOutcomes",
                c => new
                    {
                        LtiOutcomeId = c.Int(nullable: false, identity: true),
                        LtiConsumerId = c.Int(nullable: false),
                        ContextTitle = c.String(),
                        LisResultSourcedId = c.String(),
                        ServiceUrl = c.String(),
                        DateCreated = c.DateTime(nullable: false),
                        DateModified = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.LtiOutcomeId)
                .ForeignKey("dbo.LtiConsumers", t => t.LtiConsumerId, cascadeDelete: true)
                .Index(t => t.LtiConsumerId);
            
            CreateTable(
                "dbo.LtiProviderRequests",
                c => new
                    {
                        LtiProviderRequestId = c.Int(nullable: false, identity: true),
                        LtiRequest = c.String(),
                        Received = c.DateTime(nullable: false),
                        DateCreated = c.DateTime(nullable: false),
                        DateModified = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.LtiProviderRequestId);
            
            CreateTable(
                "dbo.AspNetRoles",
                c => new
                    {
                        Id = c.String(nullable: false, maxLength: 128),
                        Name = c.String(nullable: false, maxLength: 256),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.Name, unique: true, name: "RoleNameIndex");
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.AspNetUserRoles", "RoleId", "dbo.AspNetRoles");
            DropForeignKey("dbo.LtiOutcomes", "LtiConsumerId", "dbo.LtiConsumers");
            DropForeignKey("dbo.AspNetUserRoles", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.AspNetUserLogins", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.Enrollments", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.AspNetUserClaims", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.StudentSubmissions", "StudentAssignmentId", "dbo.StudentAssignments");
            DropForeignKey("dbo.StudentAssignments", "EnrollmentId", "dbo.Enrollments");
            DropForeignKey("dbo.StudentAssignments", "AssignmentId", "dbo.Assignments");
            DropForeignKey("dbo.Enrollments", "CourseId", "dbo.Courses");
            DropForeignKey("dbo.Assignments", "CourseId", "dbo.Courses");
            DropForeignKey("dbo.Pages", "AssignmentId", "dbo.Assignments");
            DropForeignKey("dbo.Scripts", "PageId", "dbo.Pages");
            DropForeignKey("dbo.Scripts", "AssignmentId", "dbo.Assignments");
            DropIndex("dbo.AspNetRoles", "RoleNameIndex");
            DropIndex("dbo.LtiOutcomes", new[] { "LtiConsumerId" });
            DropIndex("dbo.AspNetUserRoles", new[] { "RoleId" });
            DropIndex("dbo.AspNetUserRoles", new[] { "UserId" });
            DropIndex("dbo.AspNetUserLogins", new[] { "UserId" });
            DropIndex("dbo.AspNetUserClaims", new[] { "UserId" });
            DropIndex("dbo.AspNetUsers", "UserNameIndex");
            DropIndex("dbo.StudentSubmissions", new[] { "StudentAssignmentId" });
            DropIndex("dbo.StudentAssignments", new[] { "EnrollmentId" });
            DropIndex("dbo.StudentAssignments", new[] { "AssignmentId" });
            DropIndex("dbo.Enrollments", new[] { "LtiUserId" });
            DropIndex("dbo.Enrollments", new[] { "UserId" });
            DropIndex("dbo.Enrollments", new[] { "CourseId" });
            DropIndex("dbo.Courses", new[] { "LtiContextId" });
            DropIndex("dbo.Pages", new[] { "AssignmentId" });
            DropIndex("dbo.Scripts", new[] { "PageId" });
            DropIndex("dbo.Scripts", new[] { "AssignmentId" });
            DropIndex("dbo.Assignments", new[] { "LtiResourceLinkId" });
            DropIndex("dbo.Assignments", new[] { "CourseId" });
            DropTable("dbo.AspNetRoles");
            DropTable("dbo.LtiProviderRequests");
            DropTable("dbo.LtiOutcomes");
            DropTable("dbo.LtiConsumers");
            DropTable("dbo.AspNetUserRoles");
            DropTable("dbo.AspNetUserLogins");
            DropTable("dbo.AspNetUserClaims");
            DropTable("dbo.AspNetUsers");
            DropTable("dbo.StudentSubmissions");
            DropTable("dbo.StudentAssignments");
            DropTable("dbo.Enrollments");
            DropTable("dbo.Courses");
            DropTable("dbo.Pages");
            DropTable("dbo.Scripts");
            DropTable("dbo.Assignments");
        }
    }
}
