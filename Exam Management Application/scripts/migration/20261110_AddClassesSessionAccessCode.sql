/*
  Additive target-schema proposal only. Run against a disposable/test database
  after reviewing current schema and resolving the one-attempt policy.
  This script intentionally does not drop SessionCandidates or alter production
  data. AccessCode is nullable until existing sessions receive generated codes.
*/
IF OBJECT_ID(N'dbo.Classes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Classes
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Classes PRIMARY KEY,
        Code nvarchar(50) NOT NULL,
        Name nvarchar(200) NOT NULL,
        SubjectId int NOT NULL,
        Term nvarchar(50) NULL,
        IsActive bit NOT NULL CONSTRAINT DF_Classes_IsActive DEFAULT (1),
        CONSTRAINT FK_Classes_Subjects FOREIGN KEY (SubjectId) REFERENCES dbo.Subjects(Id),
        CONSTRAINT UQ_Classes_Subject_Code UNIQUE (SubjectId, Code)
    );
END;
IF OBJECT_ID(N'dbo.ClassStudents', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ClassStudents
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ClassStudents PRIMARY KEY,
        ClassId int NOT NULL,
        StudentId int NOT NULL,
        AddedAt datetime2 NOT NULL CONSTRAINT DF_ClassStudents_AddedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_ClassStudents_Classes FOREIGN KEY (ClassId) REFERENCES dbo.Classes(Id),
        CONSTRAINT FK_ClassStudents_Users FOREIGN KEY (StudentId) REFERENCES dbo.Users(UserID),
        CONSTRAINT UQ_ClassStudents_Class_Student UNIQUE (ClassId, StudentId)
    );
END;
IF OBJECT_ID(N'dbo.SessionClasses', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SessionClasses
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_SessionClasses PRIMARY KEY,
        SessionId int NOT NULL,
        ClassId int NOT NULL,
        CONSTRAINT FK_SessionClasses_ExamSessions FOREIGN KEY (SessionId) REFERENCES dbo.ExamSessions(Id),
        CONSTRAINT FK_SessionClasses_Classes FOREIGN KEY (ClassId) REFERENCES dbo.Classes(Id),
        CONSTRAINT UQ_SessionClasses_Session_Class UNIQUE (SessionId, ClassId)
    );
END;
IF COL_LENGTH(N'dbo.ExamSessions', N'AccessCode') IS NULL
    ALTER TABLE dbo.ExamSessions ADD AccessCode nvarchar(32) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.ExamSessions') AND name=N'UX_ExamSessions_AccessCode')
    CREATE UNIQUE INDEX UX_ExamSessions_AccessCode ON dbo.ExamSessions(AccessCode) WHERE AccessCode IS NOT NULL;

/*
Optional sample-only mapping template. Replace values after reviewing the
actual demo roster; never infer enrollment from past exam assignments.

INSERT dbo.Classes(Code, Name, SubjectId, Term)
VALUES (N'DEMO-01', N'Lớp thử nghiệm', <SubjectId>, N'2026-1');
INSERT dbo.ClassStudents(ClassId, StudentId)
SELECT <ClassId>, <StudentUserID>;

Before deprecating SessionCandidates, reconcile each historical candidate with
an explicit Classes/ClassStudents relationship and SessionClasses mapping.
Keep the old table for historical Attempts until the team approves cutover.
*/
