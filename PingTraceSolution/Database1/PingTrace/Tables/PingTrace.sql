﻿CREATE TABLE [PingTrace].[PingTraceHistory]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [Who] NVARCHAR(50) NOT NULL, 
    [When] DATETIME NOT NULL DEFAULT getdate()
)
