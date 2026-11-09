# Online / Local Quiz Management System

University project - Modern Development (CS312). A small quiz program written in C# with Windows Forms (.NET 8).

## Group members

| Student ID | Name |
|---|---|
| 200054085 | Aly Ahmed Abdallah |
| 200049751 | Khaled Tamer Shafik |
| 200054052 | Abdelrahman Osama Osman |
| 200054036 | Ahmed Mohamed Ali |
| 200060144 | Ahmed Mohamed Fawzy |

## What it does

- Login (username or email, remember me) and registration
- Dashboard with quiz counts and recent results
- Admin: add / edit / delete quizzes, questions (4 types) and users, see reports, export results to CSV
- Student: take quizzes with a countdown timer, see score, pass/fail and correct/wrong answers
- Settings: profile, change password, logout

Admin also works as the teacher. New registered accounts are Students; an admin can create other admins.

## Question types

| Type | How the student answers | How it is marked |
|---|---|---|
| Multiple choice | pick one of 4 answers | the picked answer must be the right one |
| True / False | pick True or False | must match |
| Short answer | types the answer | must match one of the accepted answers (not case sensitive) |
| Multiple answers | ticks all the right answers | the ticked set must be exactly the right set |

## Local mode and online mode

- **Local mode works.** Data is saved in `quizdata.json` next to the program.
- **Online mode is not built.** There is no server. The code only keeps the place for it (`IStorage` / `OnlineStorage` in `Db.cs`). Choosing it in Settings just shows a message.

## How to run

1. Install Visual Studio 2022 Community (free) with the ".NET desktop development" workload: https://visualstudio.microsoft.com/downloads/
2. Open `QuizApp.sln`
3. Press F5

Demo accounts: `admin / Admin@123` and `student / Student@123`.
If you used an older version, delete the old `quizdata.json` first so the new sample questions are created.
To reset the data, close the program and delete `quizdata.json` in `bin/Debug/net8.0-windows/`.

## Files

| File | What is inside |
|---|---|
| `Program.cs` | starts the app: login, then main window |
| `Models.cs` | User, Quiz, Question, Result classes |
| `Db.cs` | saving/loading, login, checks, sample data |
| `Helpers.cs` | small shortcuts to create labels, buttons, lists |
| `AuthForms.cs` | login and register windows |
| `MainForm.cs` | main window with all the pages (tabs on the left) |
| `QuizForm.cs` | the window where a student answers a quiz |

## Screenshots

Add them in a `screenshots` folder after running the program.

## Limits

- No essay questions yet (they need a teacher to mark them by hand)
- Pass mark is fixed at 50%
- Password uses a simple SHA-256 hash with a fixed text added (a real system should use a salt per user)
