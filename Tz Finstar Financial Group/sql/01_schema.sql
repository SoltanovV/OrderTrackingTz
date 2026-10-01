-- SQL Server 2016 SP1+; run in a dedicated database for the second assignment.
IF OBJECT_ID(N'dbo.ClientPayments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ClientPayments
    (
        Id bigint NOT NULL CONSTRAINT PK_ClientPayments PRIMARY KEY,
        ClientId bigint NOT NULL,
        Dt datetime2(0) NOT NULL,
        Amount money NOT NULL
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE object_id = OBJECT_ID(N'dbo.ClientPayments') AND name = N'IX_ClientPayments_ClientId_Dt')
    CREATE INDEX IX_ClientPayments_ClientId_Dt ON dbo.ClientPayments(ClientId, Dt) INCLUDE (Amount);
GO
