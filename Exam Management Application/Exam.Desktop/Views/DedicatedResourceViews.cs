namespace Exam.Desktop;

public sealed class QuestionBankView(ExamApiClient api) : ResourceView(api, "questions");
public sealed class ExamPapersView(ExamApiClient api) : ResourceView(api, "exams");
public sealed class ExamSessionsView(ExamApiClient api) : ResourceView(api, "sessions");
public sealed class UsersView(ExamApiClient api) : ResourceView(api, "users");
public sealed class TopicsView(ExamApiClient api) : ResourceView(api, "topics");
public sealed class SubjectsView(ExamApiClient api) : ResourceView(api, "subjects");
public sealed class AssignmentsView(ExamApiClient api) : ResourceView(api, "assignments");
public sealed class CandidatesView(ExamApiClient api) : ResourceView(api, "candidates");
public sealed class MonitorView(ExamApiClient api) : ResourceView(api, "monitor");
