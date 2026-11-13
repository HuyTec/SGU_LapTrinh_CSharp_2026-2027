using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
namespace Exam.Data;

public class ExamDbContext(DbContextOptions<ExamDbContext> options) : IdentityDbContext<AppUser, AppRole, int>(options)
{
    public DbSet<AuthSession> AuthSessions => Set<AuthSession>(); public DbSet<Subject> Subjects => Set<Subject>(); public DbSet<LecturerSubject> LecturerSubjects => Set<LecturerSubject>(); public DbSet<Topic> Topics => Set<Topic>(); public DbSet<Question> Questions => Set<Question>(); public DbSet<AnswerOption> AnswerOptions => Set<AnswerOption>(); public DbSet<ExamPaper> Exams => Set<ExamPaper>(); public DbSet<ExamQuestion> ExamQuestions => Set<ExamQuestion>(); public DbSet<ExamSession> ExamSessions => Set<ExamSession>(); public DbSet<SessionCandidate> SessionCandidates => Set<SessionCandidate>(); public DbSet<Attempt> Attempts => Set<Attempt>(); public DbSet<AttemptQuestion> AttemptQuestions => Set<AttemptQuestion>(); public DbSet<AttemptOption> AttemptOptions => Set<AttemptOption>(); public DbSet<Response> Responses => Set<Response>(); public DbSet<ExamResult> ExamResults => Set<ExamResult>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<AppUser>().ToTable("Users");
        b.Entity<AppUser>().Property(x => x.Id).HasColumnName("UserID");
        b.Entity<AppRole>().ToTable("Roles");
        b.Entity<AppRole>().Property(x => x.Id).HasColumnName("RoleID");
        b.Entity<IdentityUserRole<int>>().ToTable("UserRoles");
        b.Entity<IdentityUserRole<int>>().Property(x => x.UserId).HasColumnName("UserID");
        b.Entity<IdentityUserRole<int>>().Property(x => x.RoleId).HasColumnName("RoleID");
        b.Entity<Question>().HasOne(x => x.Creator).WithMany().HasForeignKey(x => x.CreatedBy);
        b.Entity<ExamPaper>().ToTable("Exams").HasOne(x => x.Creator).WithMany().HasForeignKey(x => x.CreatedBy);
        b.Entity<ExamSession>().HasOne(x => x.Manager).WithMany().HasForeignKey(x => x.ManagedBy);
        b.Entity<AttemptQuestion>().HasOne(x => x.Response).WithOne(x => x.Question).HasForeignKey<Response>(x => x.AttemptQuestionId);
        b.Entity<Attempt>().HasOne(x => x.Result).WithOne(x => x.Attempt).HasForeignKey<ExamResult>(x => x.AttemptId);
        b.Entity<Subject>().HasIndex(x => x.Code).IsUnique();
        b.Entity<AppUser>().HasIndex(x => x.StudentCode).IsUnique().HasFilter("[StudentCode] IS NOT NULL");
        b.Entity<LecturerSubject>().HasIndex(x => new { x.LecturerId, x.SubjectId }).IsUnique();
        b.Entity<SessionCandidate>().HasIndex(x => new { x.SessionId, x.StudentId }).IsUnique();
        b.Entity<ExamQuestion>().HasIndex(x => new { x.ExamId, x.QuestionId }).IsUnique();
        b.Entity<Attempt>().HasIndex(x => new { x.SessionId, x.StudentId }).IsUnique();
        b.Entity<ExamResult>().HasIndex(x => x.AttemptId).IsUnique();
        foreach (var e in b.Model.GetEntityTypes())
        {
            foreach (var f in e.GetForeignKeys())
                f.DeleteBehavior = DeleteBehavior.Restrict;
            foreach (var p in e.GetProperties())
            {
                if (p.ClrType == typeof(decimal))
                {
                    p.SetPrecision(12);
                    p.SetScale(4);
                }
                if (p.ClrType == typeof(string) && p.GetMaxLength() == null)
                    p.SetMaxLength(2000);
            }
        }
        b.Entity<Subject>().Property(x => x.Code).HasMaxLength(30);
        b.Entity<Subject>().Property(x => x.Name).HasMaxLength(150);
        b.Entity<AppUser>().Property(x => x.StudentCode).HasMaxLength(30);
        b.Entity<AppUser>().Property(x => x.RequestedRole).HasMaxLength(32);
        b.Entity<Topic>().Property(x => x.Name).HasMaxLength(150);
        b.Entity<Topic>().HasIndex(x => new { x.SubjectId, x.Name }).IsUnique();
        b.Entity<AnswerOption>().HasIndex(x => new { x.QuestionId, x.Order }).IsUnique();
        b.Entity<AnswerOption>().HasIndex(x => x.QuestionId).IsUnique().HasFilter("[IsCorrect] = 1");
        b.Entity<ExamQuestion>().HasIndex(x => new { x.ExamId, x.Order }).IsUnique();
        b.Entity<AttemptQuestion>().HasIndex(x => new { x.AttemptId, x.Order }).IsUnique();
        b.Entity<AttemptOption>().HasIndex(x => new { x.AttemptQuestionId, x.Order }).IsUnique();
        b.Entity<SessionCandidate>().HasAlternateKey(x => new { x.SessionId, x.StudentId });
        b.Entity<ExamSession>().Property(x => x.AccessCode).HasMaxLength(32);
        b.Entity<ExamSession>().HasIndex(x => x.AccessCode).IsUnique();
        b.Entity<ExamClass>().ToTable("Classes");
        b.Entity<ExamClass>().Property(x => x.Code).HasMaxLength(50);
        b.Entity<ExamClass>().HasIndex(x => new { x.SubjectId, x.Code }).IsUnique();
        b.Entity<ExamClass>().HasOne<Subject>().WithMany().HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ClassStudent>().ToTable("ClassStudents").HasKey(x => new { x.ClassId, x.StudentId });
        b.Entity<ClassStudent>().HasOne<AppUser>().WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<SessionClass>().ToTable("SessionClasses").HasKey(x => new { x.SessionId, x.ClassId });
        b.Entity<ExamSession>().HasMany(x => x.Classes).WithOne().HasForeignKey(x => x.SessionId);
        b.Entity<SessionClass>().HasOne(x => x.Class).WithMany().HasForeignKey(x => x.ClassId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<AttemptOption>().HasAlternateKey(x => new { x.AttemptQuestionId, x.Id });
        b.Entity<Response>().HasOne(x => x.SelectedOption).WithMany().HasForeignKey(x => new { x.AttemptQuestionId, x.SelectedOptionId }).HasPrincipalKey(x => new { x.AttemptQuestionId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<AttemptQuestion>().HasOne<Question>().WithMany().HasForeignKey(x => x.SourceQuestionId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ExamResult>().Property(x => x.Score).HasPrecision(5, 2);
        b.Entity<ExamQuestion>().Property(x => x.Points).HasPrecision(8, 2);
        b.Entity<AttemptQuestion>().Property(x => x.Points).HasPrecision(8, 2);
        b.Entity<ExamSession>().ToTable("ExamSessions", t => { t.HasCheckConstraint("CK_Session_Time", "[OpensAt] < [ClosesAt] AND [DurationMinutes] > 0"); t.HasCheckConstraint("CK_Session_Status", "[Status] IN ('Draft','Active','Cancelled')"); t.HasCheckConstraint("CK_Session_Review", "[ReviewPolicy] IN ('ScoreOnly','Answers','CorrectAnswers')"); });
        b.Entity<Attempt>().ToTable("Attempts", t => { t.HasCheckConstraint("CK_Attempt_Time", "[StartedAt] < [Deadline]"); t.HasCheckConstraint("CK_Attempt_Status", "[Status] IN ('InProgress','Submitted')"); });
        b.Entity<ExamResult>().ToTable("ExamResults", t => t.HasCheckConstraint("CK_Result_Score", "[Score] >= 0 AND [Score] <= 10"));
        b.Entity<ExamQuestion>().ToTable("ExamQuestions", t => t.HasCheckConstraint("CK_ExamQuestion_Points", "[Points] > 0"));
        b.Entity<AttemptQuestion>().ToTable("AttemptQuestions", t => t.HasCheckConstraint("CK_AttemptQuestion_Points", "[Points] > 0"));
        b.Entity<Question>().Property(x => x.RowVersion).IsRowVersion();
        b.Entity<Question>().ToTable("Questions", t => t.HasCheckConstraint("CK_Question_Difficulty", "[Difficulty] IN ('Easy','Medium','Hard')"));
        b.Entity<ExamPaper>().ToTable("Exams", t => t.HasCheckConstraint("CK_Exam_Status", "[Status] IN ('Draft','Ready')"));
    }
}



