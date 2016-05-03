CREATE PROCEDURE [PingTrace].[spTraces]
AS
	DECLARE @Id uniqueidentifier = '{188712E4-4E7E-417E-8C91-D9115BC0BE22}'
	DECLARE @Name varchar(50) = 'Database1'
	DECLARE @StartedAt datetime = getdate()
	DECLARE @FinishedAt datetime
	DECLARE @Payload varchar(50) = 'Pong'
	
	SELECT 
        [Id] = @Id,
        [Name] = @Name,
        [ExpectedIdentity] = null,
        [ExpectedMachineName] = 'SIAD049W\SQLEXPRESS',
        [ElapsedMaxSeconds] = 1,
        [ElapsedAverageSeconds] = 1,
        [PayloadDescription] = 'Pong',
        [PayloadRegex] = 'Pong'
RETURN 0
