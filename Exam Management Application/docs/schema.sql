IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE TABLE [Roles] (
        [RoleID] int NOT NULL IDENTITY,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(2000) NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY ([RoleID])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE TABLE [Subjects] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(30) NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Subjects] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE TABLE [Users] (
        [UserID] int NOT NULL IDENTITY,
        [FullName] nvarchar(2000) NOT NULL,
        [StudentCode] nvarchar(30) NULL,
        [IsActive] bit NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(2000) NULL,
        [SecurityStamp] nvarchar(2000) NULL,
        [ConcurrencyStamp] nvarchar(2000) NULL,
        [PhoneNumber] nvarchar(2000) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([UserID])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] int NOT NULL,
        [ClaimType] nvarchar(2000) NULL,
        [ClaimValue] nvarchar(2000) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([RoleID]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE TABLE [Topics] (
        [Id] int NOT NULL IDENTITY,
        [SubjectId] int NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Topics] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Topics_Subjects_SubjectId] FOREIGN KEY ([SubjectId]) REFERENCES [Subjects] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [ClaimType] nvarchar(2000) NULL,
        [ClaimValue] nvarchar(2000) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserID]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(2000) NOT NULL,
        [ProviderKey] nvarchar(2000) NOT NULL,
        [ProviderDisplayName] nvarchar(2000) NULL,
        [UserId] int NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserID]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] int NOT NULL,
        [LoginProvider] nvarchar(2000) NOT NULL,
        [Name] nvarchar(2000) NOT NULL,
        [Value] nvarchar(2000) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserID]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE TABLE [AuthSessions] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] int NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [Revoked] bit NOT NULL,
        CONSTRAINT [PK_AuthSessions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AuthSessions_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserID]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE TABLE [Exams] (
        [Id] int NOT NULL IDENTITY,
        [SubjectId] int NOT NULL,
        [CreatedBy] int NOT NULL,
        [Title] nvarchar(2000) NOT NULL,
        [Status] nvarchar(2000) NOT NULL,
        CONSTRAINT [PK_Exams] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Exam_Status] CHECK ([Status] IN ('Draft','Ready')),
        CONSTRAINT [FK_Exams_Subjects_SubjectId] FOREIGN KEY ([SubjectId]) REFERENCES [Subjects] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Exams_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([UserID]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE TABLE [LecturerSubjects] (
        [Id] int NOT NULL IDENTITY,
        [LecturerId] int NOT NULL,
        [SubjectId] int NOT NULL,
        CONSTRAINT [PK_LecturerSubjects] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LecturerSubjects_Subjects_SubjectId] FOREIGN KEY ([SubjectId]) REFERENCES [Subjects] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_LecturerSubjects_Users_LecturerId] FOREIGN KEY ([LecturerId]) REFERENCES [Users] ([UserID]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE TABLE [UserRoles] (
        [UserID] int NOT NULL,
        [RoleID] int NOT NULL,
        CONSTRAINT [PK_UserRoles] PRIMARY KEY ([UserID], [RoleID]),
        CONSTRAINT [FK_UserRoles_Roles_RoleID] FOREIGN KEY ([RoleID]) REFERENCES [Roles] ([RoleID]) ON DELETE NO ACTION,
        CONSTRAINT [FK_UserRoles_Users_UserID] FOREIGN KEY ([UserID]) REFERENCES [Users] ([UserID]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE TABLE [Questions] (
        [Id] int NOT NULL IDENTITY,
        [TopicId] int NOT NULL,
        [CreatedBy] int NOT NULL,
        [Text] nvarchar(2000) NOT NULL,
        [Difficulty] nvarchar(2000) NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Questions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Question_Difficulty] CHECK ([Difficulty] IN ('Easy','Medium','Hard')),
        CONSTRAINT [FK_Questions_Topics_TopicId] FOREIGN KEY ([TopicId]) REFERENCES [Topics] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Questions_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([UserID]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE TABLE [ExamSessions] (
        [Id] int NOT NULL IDENTITY,
        [ExamId] int NOT NULL,
        [ManagedBy] int NOT NULL,
        [Title] nvarchar(2000) NOT NULL,
        [OpensAt] datetime2 NOT NULL,
        [ClosesAt] datetime2 NOT NULL,
        [DurationMinutes] int NOT NULL,
        [Status] nvarchar(2000) NOT NULL,
        [ShuffleQuestions] bit NOT NULL,
        [ShuffleOptions] bit NOT NULL,
        [ReviewPolicy] nvarchar(2000) NOT NULL,
        [Published] bit NOT NULL,
        [HasOpened] bit NOT NULL,
        CONSTRAINT [PK_ExamSessions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Session_Review] CHECK ([ReviewPolicy] IN ('ScoreOnly','Answers','CorrectAnswers')),
        CONSTRAINT [CK_Session_Status] CHECK ([Status] IN ('Draft','Active','Cancelled')),
        CONSTRAINT [CK_Session_Time] CHECK ([OpensAt] < [ClosesAt] AND [DurationMinutes] > 0),
        CONSTRAINT [FK_ExamSessions_Exams_ExamId] FOREIGN KEY ([ExamId]) REFERENCES [Exams] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ExamSessions_Users_ManagedBy] FOREIGN KEY ([ManagedBy]) REFERENCES [Users] ([UserID]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE TABLE [AnswerOptions] (
        [Id] int NOT NULL IDENTITY,
        [QuestionId] int NOT NULL,
        [Text] nvarchar(2000) NOT NULL,
        [IsCorrect] bit NOT NULL,
        [Order] int NOT NULL,
        CONSTRAINT [PK_AnswerOptions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AnswerOptions_Questions_QuestionId] FOREIGN KEY ([QuestionId]) REFERENCES [Questions] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE TABLE [ExamQuestions] (
        [Id] int NOT NULL IDENTITY,
        [ExamId] int NOT NULL,
        [QuestionId] int NOT NULL,
        [Order] int NOT NULL,
        [Points] decimal(8,2) NOT NULL,
        CONSTRAINT [PK_ExamQuestions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ExamQuestion_Points] CHECK ([Points] > 0),
        CONSTRAINT [FK_ExamQuestions_Exams_ExamId] FOREIGN KEY ([ExamId]) REFERENCES [Exams] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ExamQuestions_Questions_QuestionId] FOREIGN KEY ([QuestionId]) REFERENCES [Questions] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE TABLE [SessionCandidates] (
        [Id] int NOT NULL IDENTITY,
        [SessionId] int NOT NULL,
        [StudentId] int NOT NULL,
        CONSTRAINT [PK_SessionCandidates] PRIMARY KEY ([Id]),
        CONSTRAINT [AK_SessionCandidates_SessionId_StudentId] UNIQUE ([SessionId], [StudentId]),
        CONSTRAINT [FK_SessionCandidates_ExamSessions_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [ExamSessions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SessionCandidates_Users_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Users] ([UserID]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE TABLE [Attempts] (
        [Id] bigint NOT NULL IDENTITY,
        [SessionId] int NOT NULL,
        [StudentId] int NOT NULL,
        [StartedAt] datetime2 NOT NULL,
        [Deadline] datetime2 NOT NULL,
        [SubmittedAt] datetime2 NULL,
        [Status] nvarchar(2000) NOT NULL,
        [EndReason] nvarchar(2000) NULL,
        CONSTRAINT [PK_Attempts] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Attempt_Status] CHECK ([Status] IN ('InProgress','Submitted')),
        CONSTRAINT [CK_Attempt_Time] CHECK ([StartedAt] < [Deadline]),
        CONSTRAINT [FK_Attempts_ExamSessions_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [ExamSessions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Attempts_SessionCandidates_SessionId_StudentId] FOREIGN KEY ([SessionId], [StudentId]) REFERENCES [SessionCandidates] ([SessionId], [StudentId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Attempts_Users_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Users] ([UserID]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE TABLE [AttemptQuestions] (
        [Id] bigint NOT NULL IDENTITY,
        [AttemptId] bigint NOT NULL,
        [SourceQuestionId] int NOT NULL,
        [Text] nvarchar(2000) NOT NULL,
        [Order] int NOT NULL,
        [Points] decimal(8,2) NOT NULL,
        CONSTRAINT [PK_AttemptQuestions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_AttemptQuestion_Points] CHECK ([Points] > 0),
        CONSTRAINT [FK_AttemptQuestions_Attempts_AttemptId] FOREIGN KEY ([AttemptId]) REFERENCES [Attempts] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AttemptQuestions_Questions_SourceQuestionId] FOREIGN KEY ([SourceQuestionId]) REFERENCES [Questions] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE TABLE [ExamResults] (
        [Id] bigint NOT NULL IDENTITY,
        [AttemptId] bigint NOT NULL,
        [Score] decimal(5,2) NOT NULL,
        [CorrectCount] int NOT NULL,
        [GradedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ExamResults] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Result_Score] CHECK ([Score] >= 0 AND [Score] <= 10),
        CONSTRAINT [FK_ExamResults_Attempts_AttemptId] FOREIGN KEY ([AttemptId]) REFERENCES [Attempts] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE TABLE [AttemptOptions] (
        [Id] bigint NOT NULL IDENTITY,
        [AttemptQuestionId] bigint NOT NULL,
        [Text] nvarchar(2000) NOT NULL,
        [Order] int NOT NULL,
        [IsCorrect] bit NOT NULL,
        CONSTRAINT [PK_AttemptOptions] PRIMARY KEY ([Id]),
        CONSTRAINT [AK_AttemptOptions_AttemptQuestionId_Id] UNIQUE ([AttemptQuestionId], [Id]),
        CONSTRAINT [FK_AttemptOptions_AttemptQuestions_AttemptQuestionId] FOREIGN KEY ([AttemptQuestionId]) REFERENCES [AttemptQuestions] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE TABLE [Responses] (
        [Id] bigint NOT NULL IDENTITY,
        [AttemptQuestionId] bigint NOT NULL,
        [SelectedOptionId] bigint NULL,
        [IsFlagged] bit NOT NULL,
        [SavedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Responses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Responses_AttemptOptions_AttemptQuestionId_SelectedOptionId] FOREIGN KEY ([AttemptQuestionId], [SelectedOptionId]) REFERENCES [AttemptOptions] ([AttemptQuestionId], [Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Responses_AttemptQuestions_AttemptQuestionId] FOREIGN KEY ([AttemptQuestionId]) REFERENCES [AttemptQuestions] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_AnswerOptions_QuestionId] ON [AnswerOptions] ([QuestionId]) WHERE [IsCorrect] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AnswerOptions_QuestionId_Order] ON [AnswerOptions] ([QuestionId], [Order]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AttemptOptions_AttemptQuestionId_Order] ON [AttemptOptions] ([AttemptQuestionId], [Order]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AttemptQuestions_AttemptId_Order] ON [AttemptQuestions] ([AttemptId], [Order]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_AttemptQuestions_SourceQuestionId] ON [AttemptQuestions] ([SourceQuestionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Attempts_SessionId_StudentId] ON [Attempts] ([SessionId], [StudentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_Attempts_StudentId] ON [Attempts] ([StudentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_AuthSessions_UserId] ON [AuthSessions] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ExamQuestions_ExamId_Order] ON [ExamQuestions] ([ExamId], [Order]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ExamQuestions_ExamId_QuestionId] ON [ExamQuestions] ([ExamId], [QuestionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_ExamQuestions_QuestionId] ON [ExamQuestions] ([QuestionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ExamResults_AttemptId] ON [ExamResults] ([AttemptId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_Exams_CreatedBy] ON [Exams] ([CreatedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_Exams_SubjectId] ON [Exams] ([SubjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_ExamSessions_ExamId] ON [ExamSessions] ([ExamId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_ExamSessions_ManagedBy] ON [ExamSessions] ([ManagedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_LecturerSubjects_LecturerId_SubjectId] ON [LecturerSubjects] ([LecturerId], [SubjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_LecturerSubjects_SubjectId] ON [LecturerSubjects] ([SubjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_Questions_CreatedBy] ON [Questions] ([CreatedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_Questions_TopicId] ON [Questions] ([TopicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Responses_AttemptQuestionId] ON [Responses] ([AttemptQuestionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_Responses_AttemptQuestionId_SelectedOptionId] ON [Responses] ([AttemptQuestionId], [SelectedOptionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [Roles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SessionCandidates_SessionId_StudentId] ON [SessionCandidates] ([SessionId], [StudentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_SessionCandidates_StudentId] ON [SessionCandidates] ([StudentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Subjects_Code] ON [Subjects] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Topics_SubjectId_Name] ON [Topics] ([SubjectId], [Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_UserRoles_RoleID] ON [UserRoles] ([RoleID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [Users] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Users_StudentCode] ON [Users] ([StudentCode]) WHERE [StudentCode] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [Users] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024073833_InitialSchema'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261024073833_InitialSchema', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024074450_PublicationTimestamp'
)
BEGIN
    ALTER TABLE [ExamSessions] ADD [PublishedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024074450_PublicationTimestamp'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261024074450_PublicationTimestamp', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024095847_QuestionConcurrency'
)
BEGIN
    ALTER TABLE [Questions] ADD [RowVersion] rowversion NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024095847_QuestionConcurrency'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261024095847_QuestionConcurrency', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024110741_StaffRegistrationApproval'
)
BEGIN
    ALTER TABLE [Users] ADD [RequestedRole] nvarchar(32) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024110741_StaffRegistrationApproval'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261024110741_StaffRegistrationApproval', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113090207_StudentClassAdmission'
)
BEGIN
    ALTER TABLE [Attempts] DROP CONSTRAINT [FK_Attempts_SessionCandidates_SessionId_StudentId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113090207_StudentClassAdmission'
)
BEGIN
    ALTER TABLE [ExamSessions] ADD [AccessCode] nvarchar(32) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113090207_StudentClassAdmission'
)
BEGIN
    CREATE TABLE [Classes] (
        [Id] int NOT NULL IDENTITY,
        [SubjectId] int NOT NULL,
        [Code] nvarchar(50) NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Classes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Classes_Subjects_SubjectId] FOREIGN KEY ([SubjectId]) REFERENCES [Subjects] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113090207_StudentClassAdmission'
)
BEGIN
    CREATE TABLE [ClassStudents] (
        [ClassId] int NOT NULL,
        [StudentId] int NOT NULL,
        CONSTRAINT [PK_ClassStudents] PRIMARY KEY ([ClassId], [StudentId]),
        CONSTRAINT [FK_ClassStudents_Classes_ClassId] FOREIGN KEY ([ClassId]) REFERENCES [Classes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ClassStudents_Users_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Users] ([UserID]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113090207_StudentClassAdmission'
)
BEGIN
    CREATE TABLE [SessionClasses] (
        [SessionId] int NOT NULL,
        [ClassId] int NOT NULL,
        CONSTRAINT [PK_SessionClasses] PRIMARY KEY ([SessionId], [ClassId]),
        CONSTRAINT [FK_SessionClasses_Classes_ClassId] FOREIGN KEY ([ClassId]) REFERENCES [Classes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SessionClasses_ExamSessions_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [ExamSessions] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113090207_StudentClassAdmission'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ExamSessions_AccessCode] ON [ExamSessions] ([AccessCode]) WHERE [AccessCode] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113090207_StudentClassAdmission'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Classes_SubjectId_Code] ON [Classes] ([SubjectId], [Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113090207_StudentClassAdmission'
)
BEGIN
    CREATE INDEX [IX_ClassStudents_StudentId] ON [ClassStudents] ([StudentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113090207_StudentClassAdmission'
)
BEGIN
    CREATE INDEX [IX_SessionClasses_ClassId] ON [SessionClasses] ([ClassId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113090207_StudentClassAdmission'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261113090207_StudentClassAdmission', N'10.0.13');
END;

COMMIT;
GO

