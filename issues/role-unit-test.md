Perubahan:

Di Tests/Helpers/DbContextFactory.cs saya tambahkan:
using Microsoft.EntityFrameworkCore.Diagnostics;
ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
Kenapa:

RolesService.AssignPermissionsAsync() memang memanggil BeginTransactionAsync().
EF Core InMemory tidak mendukung transaction, jadi warning itu dinaikkan jadi exception saat test jalan.
Hasil verifikasi:

RolesServiceTests sudah lulus
15 passed, 0 failed
Kalau mau, saya bisa lanjut cek test lain yang mungkin masih sensitif terhadap behavior InMemory vs database relational.