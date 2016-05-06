CREATE PROCEDURE [PingTrace].[spTrace]
	@destination nvarchar(50)
AS

	DECLARE @Id uniqueidentifier = '{188712E4-4E7E-417E-8C91-D9115BC0BE22}'
	DECLARE @Name varchar(50) = 'Database1'
	DECLARE @StartedAt datetime = getdate()
	DECLARE @FinishedAt datetime
	
	INSERT INTO 
		PingTrace.PingTraceHistory 
		(
			Who
		)
		VALUES 
		(
			CURRENT_USER
		)


	SET @FinishedAt = getdate()
	
	SELECT 	        
	        [Id] = @Id,
			[Name] = @Name,
			[Identity] = CURRENT_USER,
			[MachineName] = @@SERVERNAME,
			[StartedAt] = @StartedAt,
			[FinishedAt] = @FinishedAt,
			[Payload] = 'Pong [' + DB_NAME() + ']'
        
RETURN 0
