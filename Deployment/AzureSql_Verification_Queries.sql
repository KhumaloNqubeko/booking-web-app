-- Run these queries in the Azure SQL Query Editor after deployment.
-- Capture the result grids as Part 3 submission evidence.

SELECT [Id], [Name]
FROM [EventTypes]
ORDER BY [Id];

SELECT
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Venues'
  AND COLUMN_NAME = 'Availability';

SELECT
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Events'
  AND COLUMN_NAME = 'EventTypeId';

SELECT TOP (20)
    e.[Name] AS EventName,
    et.[Name] AS EventType,
    e.[StartDateTime],
    e.[EndDateTime]
FROM [Events] e
INNER JOIN [EventTypes] et ON et.[Id] = e.[EventTypeId]
ORDER BY e.[StartDateTime];

SELECT TOP (20)
    v.[Name] AS VenueName,
    v.[Availability],
    b.[BookingDate],
    e.[Name] AS EventName,
    et.[Name] AS EventType
FROM [Bookings] b
INNER JOIN [Venues] v ON v.[Id] = b.[VenueId]
INNER JOIN [Events] e ON e.[Id] = b.[EventId]
INNER JOIN [EventTypes] et ON et.[Id] = e.[EventTypeId]
ORDER BY b.[BookingDate] DESC;
