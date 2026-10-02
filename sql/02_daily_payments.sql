CREATE OR ALTER FUNCTION dbo.GetClientDailyPayments
(
    @ClientId bigint,
    @Sd date,
    @Ed date
)
RETURNS TABLE
AS
RETURN
(
    WITH Dates AS
    (
        SELECT DATEADD(day, value, @Sd) AS Dt
        FROM GENERATE_SERIES(0, COALESCE(DATEDIFF(day, @Sd, @Ed), -1), 1)
    ),
    DailyPayments AS
    (
        SELECT CONVERT(date, Dt) AS Dt,
               SUM(CONVERT(decimal(19,4), Amount)) AS Amount
        FROM dbo.ClientPayments
        WHERE ClientId = @ClientId
          AND Dt >= @Sd
          AND Dt <= DATEADD(second, 86399, CONVERT(datetime2(0), @Ed))
        GROUP BY CONVERT(date, Dt)
    )
    SELECT d.Dt, COALESCE(p.Amount, 0) AS Amount
    FROM Dates d
    LEFT JOIN DailyPayments p ON p.Dt = d.Dt
);
