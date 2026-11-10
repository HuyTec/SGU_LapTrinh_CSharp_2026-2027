# Backlog theo lát chức năng — kiến trúc mục tiêu

Ma trận ngang là trách nhiệm review/xuyên tầng; chủ task dọc chịu trách nhiệm tích hợp UI → API → service → DB cho lát được giao. Phân công này là đề xuất phân việc, chưa ghi nhận hoàn thành.

| Lát | Owner | Phối hợp | Checklist và tiêu chí nghiệm thu |
|---|---|---|---|
| 1. Quản lý lớp — Admin Desktop | TV4 Hào | TV2 review API/DTO; TV1 review UI | Tạo `Classes`, `ClassStudents`; API CRUD lớp và enrollment; WinForms Admin quản lý lớp; validate mã lớp trùng/môn; chỉ Admin được sửa; kiểm chứng qua SQL và API. |
| 2. Thí sinh và membership | TV1 Vũ | TV4 schema; TV3 review luồng Web | Import/thêm thí sinh; kiểm tra Student active, mã sinh viên duy nhất; thêm/xóa membership lớp; gán lớp vào đợt qua `SessionClasses`; rà quyền Admin; không tạo SessionCandidates mới. |
| 3. Nội dung thi và tổ chức — Lecturer Web | TV2 Phúc | TV3 MVC review; TV4 DB review | MVC Controller/View cho ngân hàng câu hỏi, đề và đợt trong môn được phân công; chọn đề + lớp; sinh AccessCode duy nhất; kiểm tra phạm vi môn server-side; integration test. |
| 4. Vào thi, làm bài và chấm — Student Web | TV3 Huy | TV2 DTO/API review; TV1 auth review | Nhập AccessCode/link; validate Open + membership giao SessionClasses + chưa có Attempt; transaction/idempotency; snapshot câu hỏi/phương án; autosave; submit; chấm; kết quả chỉ khi Published; test race/retry/403. |

## Ranh giới trách nhiệm ngang

TV1: identity, auth/session và tài khoản. TV2: hợp đồng API/DTO và quy tắc đề/câu hỏi. TV3: MVC/Web, UX và luồng thí sinh. TV4: schema, migrations, SQL và tích hợp. Người phụ trách ngang review chất lượng xuyên tầng; không thay owner dọc của lát.

## Giao ước DTO (khung ban đầu)

`Exam.Core/Contracts/Classes`, `Exams`, `Sessions`, `Attempts` chứa record request/DTO dùng chung. API cụ thể, validation, response DTO và authorization vẫn cần chốt trong CONTRACT trước khi implement controller/service. Không coi DTO khởi tạo là API đã hoạt động.

## Thứ tự phụ thuộc

1. Chốt unique policy một Attempt/Student/Session và migration trên DB thử nghiệm.
2. Lát 1 + schema lớp; tiếp theo Lát 2 tạo membership và SessionClasses.
3. Lát 3 có thể tạo đợt và mã; phụ thuộc schema/session contract.
4. Lát 4 tích hợp toàn luồng; giữ `SessionCandidates` trong giai đoạn tương thích, chỉ deprecated/xóa sau backfill và nghiệm thu lịch sử.
