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
    WITH
    E1(n) AS (SELECT n FROM (VALUES (0),(1),(2),(3),(4),(5),(6),(7),(8),(9)) AS d(n)),
    E2(n) AS (SELECT 0 FROM E1 a CROSS JOIN E1 b),
    E4(n) AS (SELECT 0 FROM E2 a CROSS JOIN E2 b),
    Numbers(n) AS
    (
        SELECT TOP (CASE WHEN @Sd IS NOT NULL AND @Ed >= @Sd THEN DATEDIFF(day, @Sd, @Ed) + 1 ELSE 0 END)
            CONVERT(int, ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1)
        FROM E4 a CROSS JOIN E2 b CROSS JOIN E1 c
    ),
    DailyPayments AS
    (
        SELECT CONVERT(date, p.Dt) AS Dt, SUM(CONVERT(decimal(19,4), p.Amount)) AS Amount
        FROM dbo.ClientPayments p
        WHERE p.ClientId = @ClientId
          AND p.Dt >= CONVERT(datetime2(0), @Sd)
          -- Avoid adding one day to 9999-12-31.
          AND p.Dt <= DATEADD(second, 86399, CONVERT(datetime2(0), @Ed))
        GROUP BY CONVERT(date, p.Dt)
    )
    SELECT DATEADD(day, n.n, @Sd) AS Dt,
           COALESCE(p.Amount, CONVERT(decimal(38,4), 0)) AS Amount
    FROM Numbers n
    LEFT JOIN DailyPayments p ON p.Dt = DATEADD(day, n.n, @Sd)
);
GO
-- Tables/functions do not guarantee row order; callers must use ORDER BY Dt.
-- SELECT * FROM dbo.GetClientDailyPayments(1, '20220102', '20220107') ORDER BY Dt;
