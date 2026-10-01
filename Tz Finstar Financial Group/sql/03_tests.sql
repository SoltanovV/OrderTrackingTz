-- Execute after 01_schema.sql and 02_daily_payments.sql, in a dedicated test DB.
-- All fixtures are rolled back, including on failure.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DELETE FROM dbo.ClientPayments;
    INSERT INTO dbo.ClientPayments(Id, ClientId, Dt, Amount) VALUES
      (1,1,'2022-01-03T17:24:00',100),
      (2,1,'2022-01-05T17:24:14',200),
      (3,1,'2022-01-05T18:23:34',250),
      (4,1,'2022-01-07T10:12:38',50),
      (5,2,'2022-01-05T17:24:14',278),
      (6,2,'2022-01-10T12:39:29',300);

    DECLARE @Expected1 TABLE(Dt date, Amount decimal(38,4));
    INSERT INTO @Expected1 VALUES ('20220102',0),('20220103',100),('20220104',0),('20220105',450),('20220106',0),('20220107',50);
    IF EXISTS (SELECT * FROM @Expected1 EXCEPT SELECT * FROM dbo.GetClientDailyPayments(1,'20220102','20220107'))
       OR EXISTS (SELECT * FROM dbo.GetClientDailyPayments(1,'20220102','20220107') EXCEPT SELECT * FROM @Expected1)
        THROW 51000, 'Example 1 failed', 1;

    DECLARE @Expected2 TABLE(Dt date, Amount decimal(38,4));
    INSERT INTO @Expected2 VALUES ('20220104',0),('20220105',278),('20220106',0),('20220107',0),('20220108',0),('20220109',0),('20220110',300),('20220111',0);
    IF EXISTS (SELECT * FROM @Expected2 EXCEPT SELECT * FROM dbo.GetClientDailyPayments(2,'20220104','20220111'))
       OR EXISTS (SELECT * FROM dbo.GetClientDailyPayments(2,'20220104','20220111') EXCEPT SELECT * FROM @Expected2)
        THROW 51000, 'Example 2 failed', 1;

    IF (SELECT COUNT(*) FROM dbo.GetClientDailyPayments(999,'20200101','20221231')) <> 1096
        THROW 51000, 'Multi-year or leap-year interval failed', 1;
    IF EXISTS (SELECT * FROM dbo.GetClientDailyPayments(999,'20200101','20221231') WHERE Amount <> 0)
        THROW 51000, 'Missing client must return zeros', 1;
    IF EXISTS (SELECT * FROM dbo.GetClientDailyPayments(1,'20220107','20220102'))
       OR EXISTS (SELECT * FROM dbo.GetClientDailyPayments(1,NULL,'20220102'))
       OR EXISTS (SELECT * FROM dbo.GetClientDailyPayments(1,'20220102',NULL))
        THROW 51000, 'Invalid interval must be empty', 1;

    INSERT INTO dbo.ClientPayments VALUES
      (7,3,'2024-02-29T00:00:00',12.3456),
      (8,3,'2024-02-29T23:59:59',-2.0001),
      (9,3,'2024-03-01T00:00:00',500),
      (10,4,'9999-12-31T23:59:59',7);
    IF (SELECT Amount FROM dbo.GetClientDailyPayments(3,'20240229','20240229')) <> 10.3455
        THROW 51000, 'Inclusive boundaries or decimal precision failed', 1;
    IF (SELECT Amount FROM dbo.GetClientDailyPayments(4,'99991231','99991231')) <> 7
        THROW 51000, 'Maximum date failed', 1;
    IF (SELECT COUNT(*) FROM dbo.GetClientDailyPayments(99,'00010101','00010101')) <> 1
        THROW 51000, 'Minimum date failed', 1;

    SELECT * FROM dbo.GetClientDailyPayments(1,'20220102','20220107') ORDER BY Dt;
    SELECT * FROM dbo.GetClientDailyPayments(2,'20220104','20220111') ORDER BY Dt;
    ROLLBACK;
    PRINT 'All SQL checks passed.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH;
GO
