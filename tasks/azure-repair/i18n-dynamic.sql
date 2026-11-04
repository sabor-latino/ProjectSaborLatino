SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'TraduccionesI18n')
    BEGIN
        CREATE TABLE [dbo].[TraduccionesI18n] (
            [Id] int NOT NULL IDENTITY,
            [HashOrigen] nvarchar(64) NOT NULL,
            [TextoOrigen] nvarchar(2000) NOT NULL,
            [IdiomaDestino] nvarchar(10) NOT NULL,
            [TextoTraducido] nvarchar(2500) NOT NULL,
            [EsAutomatica] bit NOT NULL,
            [FechaCreacion] datetime2 NOT NULL,
            [FechaActualizacion] datetime2 NOT NULL,
            CONSTRAINT [PK_TraduccionesI18n] PRIMARY KEY ([Id])
        );

        CREATE UNIQUE INDEX [IX_TraduccionesI18n_HashOrigen_IdiomaDestino] ON [dbo].[TraduccionesI18n] ([HashOrigen], [IdiomaDestino]);
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = N'20261104212320_TraduccionesI18nDinamicas')
    BEGIN
        INSERT INTO dbo.__EFMigrationsHistory ([MigrationId], [ProductVersion])
        VALUES (N'20261104212320_TraduccionesI18nDinamicas', N'10.0.12');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
