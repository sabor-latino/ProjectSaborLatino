SET XACT_ABORT ON;
SET LOCK_TIMEOUT 15000;
BEGIN TRY
BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102190453_SolicitudWhatsAppManual'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[Solicitudes]') AND [c].[name] = N'CorreoContacto');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [dbo].[Solicitudes] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [dbo].[Solicitudes] ALTER COLUMN [CorreoContacto] nvarchar(256) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102190453_SolicitudWhatsAppManual'
)
BEGIN
    ALTER TABLE [dbo].[Solicitudes] ADD [ClaveIdempotencia] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102190453_SolicitudWhatsAppManual'
)
BEGIN
    ALTER TABLE [dbo].[Solicitudes] ADD [HuellaIdempotencia] nvarchar(64) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102190453_SolicitudWhatsAppManual'
)
BEGIN
    ALTER TABLE [dbo].[Solicitudes] ADD [RecibidoWhatsAppEn] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102190453_SolicitudWhatsAppManual'
)
BEGIN
    ALTER TABLE [dbo].[Solicitudes] ADD [RecibidoWhatsAppPorUsuarioId] nvarchar(450) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102190453_SolicitudWhatsAppManual'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Solicitudes_ClaveIdempotencia] ON [dbo].[Solicitudes] ([ClaveIdempotencia]) WHERE [ClaveIdempotencia] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102190453_SolicitudWhatsAppManual'
)
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261102190453_SolicitudWhatsAppManual', N'10.0.12');
END;

COMMIT;


END TRY
BEGIN CATCH
  IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
  THROW;
END CATCH;
