# 🎓 EduChatbot — AI-Powered Learning Platform

<div align="center">

![.NET](https://img.shields.io/badge/.NET_9-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Razor Pages](https://img.shields.io/badge/Razor_Pages-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![SignalR](https://img.shields.io/badge/SignalR-FF6C37?style=for-the-badge&logo=dotnet&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL_18-316192?style=for-the-badge&logo=postgresql&logoColor=white)
![pgvector](https://img.shields.io/badge/pgvector-316192?style=for-the-badge&logo=postgresql&logoColor=white)
![Google Gemini](https://img.shields.io/badge/Google_Gemini-4285F4?style=for-the-badge&logo=google&logoColor=white)

**Hệ thống quản lý môn học & chatbot thông minh dùng công nghệ RAG, thời gian thực với SignalR**

</div>

---

## 📋 Mục lục

- [Giới thiệu](#-giới-thiệu)
- [Tính năng nổi bật](#-tính-năng-nổi-bật)
- [Tính năng thời gian thực (SignalR)](#-tính-năng-thời-gian-thực-signalr)
- [Kiến trúc hệ thống](#️-kiến-trúc-hệ-thống)
- [Công nghệ sử dụng](#️-công-nghệ-sử-dụng)
- [Cài đặt & Chạy dự án](#-cài-đặt--chạy-dự-án)
- [Cấu trúc dự án](#-cấu-trúc-dự-án)
- [Phân quyền người dùng](#-phân-quyền-người-dùng)
- [Tài khoản mặc định](#-tài-khoản-mặc-định)
- [Bảo mật](#-bảo-mật)
- [Lưu ý](#-lưu-ý)

---

## 🌟 Giới thiệu

**EduChatbot** là ứng dụng web giáo dục xây dựng trên **ASP.NET Core 9 (Razor Pages)**, tích hợp **Google Gemini API** với công nghệ **RAG (Retrieval-Augmented Generation)** và giao tiếp **thời gian thực bằng SignalR**.

Giảng viên tải lên tài liệu (PDF, DOCX, TXT); AI tự động trích xuất, chia nhỏ (**Semantic Chunking**) và tạo **vector embedding 768 chiều**, lưu vào **PostgreSQL + pgvector**. Sinh viên đặt câu hỏi và nhận câu trả lời **streaming** dựa trên đúng nội dung tài liệu môn học, kèm **trích dẫn nguồn**.

### Luồng hoạt động RAG

```
Tài liệu (PDF / DOCX / TXT)
        │  PdfPig · OpenXml · System.IO
        ▼
  Semantic Chunking        ← chia tại ranh giới câu/đoạn, ~400 từ/chunk, overlap 2 câu
        ▼
  Gemini Embedding (768d)  ← gemini-embedding-001
        ▼
  PostgreSQL + pgvector    ← lưu vector(768)
        ▼
  ── Khi chat ──
  Câu hỏi → embedding → Cosine Similarity (top 3 chunk)
        ▼
  Gemini 2.5 Flash (prompt + ngữ cảnh) → trả lời STREAMING + nguồn trích dẫn
```

> Việc xử lý tài liệu chạy **nền (background worker)**; trạng thái cập nhật real-time qua SignalR (không cần F5).

---

## ✨ Tính năng nổi bật

### 🤖 AI & RAG
- Đọc & học tài liệu **PDF, DOCX, TXT** tự động
- **Semantic Chunking** — không cắt giữa câu, overlap theo câu
- Tìm kiếm ngữ nghĩa bằng vector embedding 768 chiều (cosine similarity)
- **Chat AI streaming**: câu trả lời hiện dần từng đoạn, render **Markdown** (đậm, danh sách, code block)
- **Trích dẫn nguồn**: mỗi câu trả lời kèm tên tài liệu đã dùng
- **Lịch sử hội thoại**: lưu DB, mở lại vẫn thấy
- **Chunk Viewer**: xem từng chunk AI đã học, thống kê số từ/chiều vector

### 📚 Quản lý học tập
- CRUD môn học, upload/xóa/tải tài liệu (chống trùng tên file)
- Xem PDF/TXT trực tiếp trên trình duyệt
- Xử lý tài liệu chạy nền, trạng thái: **Pending → Processing → Completed / Failed**

### 👥 Người dùng & phân quyền
- 3 vai trò: **Admin**, **Giảng viên**, **Sinh viên**
- Admin tạo tài khoản → tự gửi thông tin đăng nhập qua Gmail
- Gán nhiều thành viên vào môn học cùng lúc; tối đa **3 Giảng viên/Admin** mỗi môn
- **Kiểm soát truy cập theo thành viên**: chỉ Admin hoặc thành viên của môn mới xem được tài liệu/chat của môn đó

### 🔐 Bảo mật & Tài khoản
- Mật khẩu **băm BCrypt**
- Cookie Authentication + **buộc đăng xuất khi đổi quyền/xóa tài khoản** (kiểm tra mỗi request)
- Quên mật khẩu qua Gmail (link 30 phút, dùng 1 lần)
- Antiforgery (mặc định Razor Pages) + **DOMPurify** sanitize HTML câu trả lời AI

---

## ⚡ Tính năng thời gian thực (SignalR)

| Hub | Endpoint | Chức năng | Kỹ thuật SignalR |
|---|---|---|---|
| **ChatHub** | `/chatHub` | Chat AI trả lời **streaming** từng đoạn + nguồn | Server→client streaming |
| **DocumentHub** | `/documentHub` | Trạng thái xử lý tài liệu cập nhật trực tiếp | Groups (theo môn học) |
| **NotificationHub** | `/notificationHub` | 🔔 Chuông thông báo **có lưu trữ** (offline vẫn nhận) | `Clients.User` (user-targeted) |
| **SubjectHub** | `/subjectHub` | Danh sách **thành viên / môn học** tự cập nhật | Groups + partial reload |
| **DashboardHub** | `/dashboardHub` | Số liệu sống, **presence** (đang online), **broadcast** của Admin | Presence + `Clients.All` |

- **Chuông thông báo**: được thêm/gỡ khỏi môn, có tài liệu mới, Admin broadcast — đều lưu DB nên đăng nhập lại vẫn thấy.
- **Dashboard**: số môn/tài liệu/tài khoản và số người **đang online** cập nhật real-time; Admin gửi thông báo tới tất cả.

---

## 🏗️ Kiến trúc hệ thống

Dự án theo kiến trúc **3 lớp** một chiều: `Presentation → BLL → DAL`.

```
RagChatbot.RazorPages  (Presentation)  ──►  RagChatbot.BLL  (Business)  ──►  RagChatbot.DAL  (Data)
   Razor Pages, Hubs,                        Services, DTOs,                  Entities, Repositories,
   ViewComponents, BackgroundTasks           AddProjectDependencies (DI)      ApplicationDbContext, Migrations
```

- Presentation **không** tham chiếu trực tiếp DAL; mọi truy cập dữ liệu đi qua BLL services.
- BLL **không** biết SignalR/HTTP; transport real-time + background hosting nằm ở Presentation.
- Toàn bộ DI đăng ký tập trung tại `BLL/Extensions/ServiceCollectionExtensions.AddProjectDependencies`.

---

## 🛠️ Công nghệ sử dụng

| Thành phần | Công nghệ |
|---|---|
| Framework | ASP.NET Core 9 — **Razor Pages** |
| Real-time | **ASP.NET Core SignalR** |
| ORM | Entity Framework Core **9.0.4** |
| Database | PostgreSQL **18** |
| Vector store | **pgvector** (vector 768 chiều) |
| AI Chat | Google **Gemini 2.5 Flash** (streaming) |
| Embedding | Google **Gemini Embedding-001** (768d) |
| Mật khẩu | **BCrypt.Net-Next** |
| PDF / DOCX | PdfPig · DocumentFormat.OpenXml |
| Email | Gmail SMTP |
| Frontend | Bootstrap 5, Bootstrap Icons, SweetAlert2, **marked**, **DOMPurify** |

---

## 🚀 Cài đặt & Chạy dự án

### Yêu cầu
- [.NET SDK 9.0+](https://dotnet.microsoft.com/download/dotnet/9.0)
- **PostgreSQL 14+** kèm extension **pgvector**
- **Google Gemini API Key** ([lấy tại đây](https://aistudio.google.com/app/apikey))
- (Tùy chọn) Gmail + App Password để gửi email

### 1. Clone
```bash
git clone https://github.com/<your-username>/PRN222-Assiment2.git
cd PRN222-Assiment2
```

### 2. Cài pgvector cho PostgreSQL
```sql
CREATE EXTENSION IF NOT EXISTS vector;
```
> **Windows:** nếu báo `extension "vector" is not available`, cần build pgvector bằng Visual Studio C++ (Native Tools Command Prompt, chạy quyền Admin):
> ```bat
> set "PGROOT=C:\Program Files\PostgreSQL\18"
> cd %TEMP% && git clone https://github.com/pgvector/pgvector.git && cd pgvector
> nmake /F Makefile.win && nmake /F Makefile.win install
> ```

### 3. Cấu hình
Sao chép file mẫu và điền thông tin:
```bash
cp RagChatbot.RazorPages/appsettings.example.json RagChatbot.RazorPages/appsettings.json
```
Điền `ConnectionStrings:DefaultConnection` (chuỗi kết nối PostgreSQL) và `Smtp` (nếu dùng email) trong `appsettings.json`.

**Gemini API Key** — nên dùng **User Secrets** (không lưu vào file, không bị commit):
```bash
dotnet user-secrets set "Gemini:ApiKey" "YOUR_GEMINI_API_KEY" --project RagChatbot.RazorPages
```

### 4. Tạo database (chạy migration)
```bash
dotnet ef database update --project RagChatbot.DAL --startup-project RagChatbot.RazorPages
```
> Cần công cụ EF: `dotnet tool install --global dotnet-ef` (nếu chưa có).

### 5. Chạy ứng dụng
```bash
cd RagChatbot.RazorPages
dotnet run
```
Truy cập: `http://localhost:5136` (hoặc `https://localhost:7243`).

---

## 📁 Cấu trúc dự án

```
PRN222-Assiment2/
│
├── RagChatbot.DAL/                       # Data Access Layer
│   ├── Data/ApplicationDbContext.cs      # DbContext + seed users (mật khẩu BCrypt)
│   ├── Entities/                         # User, Subject, Document, DocumentChunk,
│   │                                     #   UserSubject, Notification, ChatMessage
│   ├── Repositories/ (Interfaces, Implements)
│   └── Migrations/
│
├── RagChatbot.BLL/                       # Business Logic Layer
│   ├── DTOs/                             # SubjectDto, DocumentDto, ChatMessageDto, ChatResult, ...
│   ├── Services/ (Interfaces, Implements)
│   │   ├── GeminiService.cs              # Embedding + chat streaming (Gemini)
│   │   ├── DocumentProcessingService.cs  # RAG pipeline: extract → chunk → embed → save
│   │   ├── ChatbotService.cs             # AskAsync: retrieval + nguồn + stream
│   │   ├── NotificationService.cs        # Lưu trữ thông báo
│   │   ├── ChatMessageService.cs         # Lịch sử chat
│   │   └── UserService / SubjectService / DocumentService / UserSubjectService / EmailService
│   ├── Helpers/SemanticChunker.cs
│   └── Extensions/ServiceCollectionExtensions.cs   # Đăng ký toàn bộ DI
│
└── RagChatbot.RazorPages/                # Presentation Layer
    ├── Pages/
    │   ├── Account/  (Login, Logout, AccessDenied, ForgotPassword, ResetPassword)
    │   ├── Home/     (Index — dashboard sống, Privacy)
    │   ├── Subject/  (Index, Create, Edit, _SubjectCards)
    │   ├── Document/ (Index, Create, ViewDoc, Download, _MembersTable...)
    │   ├── Member/   (Index, Add, _MembersTable)
    │   ├── Chat/     (Index — streaming + markdown + nguồn + lịch sử)
    │   ├── User/     (Index, Create, Edit)
    │   ├── Seeder/   (Index, Result)
    │   └── Shared/_Layout.cshtml         # Sidebar + chuông + SignalR client chung
    ├── Hubs/         (ChatHub, DocumentHub, NotificationHub, SubjectHub, DashboardHub)
    ├── BackgroundTasks/ (DocumentProcessingQueue + Worker)
    ├── Services/     (RealtimeNotifier, DashboardNotifier, PresenceTracker)
    ├── ViewComponents/ (NotificationBell)
    ├── wwwroot/      (css, lib, uploads — uploads KHÔNG commit)
    └── Program.cs
```

---

## 🔑 Phân quyền người dùng

| Tính năng | Admin | Giảng viên | Sinh viên |
|---|:---:|:---:|:---:|
| Xem môn học / tài liệu / chat (môn mình thuộc) | ✅ (mọi môn) | ✅ | ✅ |
| Tạo/sửa/xóa môn học | ✅ | ❌ | ❌ |
| Upload/xóa tài liệu (môn mình thuộc) | ✅ | ✅ | ❌ |
| Quản lý tài khoản | ✅ | ❌ | ❌ |
| Gán thành viên (Lecturer + Student) | ✅ | ❌ | ❌ |
| Gán/xóa thành viên (chỉ Student) | ✅ | ✅ | ❌ |
| Gửi broadcast (dashboard) | ✅ | ❌ | ❌ |

> Mỗi môn học tối đa **3 Giảng viên/Admin**. Người không thuộc môn sẽ **không** truy cập được tài liệu/chat của môn đó.

---

## 👤 Tài khoản mặc định

Tất cả mật khẩu: **`123`** (đã băm BCrypt trong DB).

| Username | Vai trò | | Username | Vai trò |
|---|---|---|---|---|
| `admin` | Admin | | `sv_bao` … `sv_duc` | Sinh viên |
| `giangvien`, `gv_minh`, `gv_lan` | Giảng viên | | `sinhvien` | Sinh viên |

---

## 🔒 Bảo mật

- **Không commit secrets**: `appsettings.json` (chuỗi kết nối DB, mật khẩu SMTP) đã nằm trong `.gitignore`; Gemini API Key lưu ở **User Secrets**. Chỉ `appsettings.example.json` (placeholder) được đẩy lên repo.
- Mật khẩu người dùng **băm BCrypt**, không lưu plaintext.
- Câu trả lời AI (Markdown→HTML) được **DOMPurify** lọc chống XSS.
- Đổi vai trò / xóa tài khoản → phiên đăng nhập của người đó bị **vô hiệu hóa ngay** (real-time + kiểm tra phía server mỗi request).

---

## 📝 Lưu ý

- Thư mục `wwwroot/uploads/` chứa file người dùng — **không commit** (đã ignore).
- Tính năng email cần **Gmail App Password** (không phải mật khẩu Gmail thường).
- Chat AI cần **Gemini API Key** hợp lệ; nếu trống, tài liệu sẽ ở trạng thái **Failed** khi xử lý.
- Khi phát triển: trước khi `dotnet run` lại, hãy **dừng bản đang chạy** (Ctrl+C / Shift+F5) để tránh lỗi khóa file DLL.

---

<div align="center">

**PRN222 Assignment 2 — FPT University**

Made with ❤️ using ASP.NET Core 9 (Razor Pages) · SignalR · Google Gemini · pgvector

</div>
