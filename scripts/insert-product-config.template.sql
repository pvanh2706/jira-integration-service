-- Jira Integration Service - product config insert/update template for SQLite.
--
-- Fill the CONFIG SECTION below, then run this file against the local database.
-- Run this script once per product.
--
-- Example with sqlite3 CLI from repo root:
-- sqlite3 src/JiraIntegrationService.Api/jira-integration.db ".read scripts/insert-product-config.template.sql"
--
-- Notes:
-- - ProductCode, IssueTypeCode, and StandardStatus are stored in upper case.
-- - IssueTypeCode = NULL means product-level fallback mapping.
-- - Delete or comment sample rows that you do not need.
-- - This script updates existing rows for the same product/code/source/status so it can be run again.

PRAGMA foreign_keys = ON;

BEGIN TRANSACTION;

-- ============================================================
-- CONFIG SECTION - edit values here.
-- ============================================================

DROP TABLE IF EXISTS temp._SeedProduct;
CREATE TEMP TABLE _SeedProduct
(
    Id INTEGER PRIMARY KEY CHECK (Id = 1),
    ProductCode TEXT NOT NULL,
    ProductName TEXT NOT NULL,
    JiraProjectKey TEXT NOT NULL,
    JiraUsername TEXT NOT NULL,
    JiraPassword TEXT NOT NULL,
    IsActive INTEGER NOT NULL DEFAULT 1 CHECK (IsActive IN (0, 1))
);

INSERT INTO _SeedProduct
(
    Id,
    ProductCode,
    ProductName,
    JiraProjectKey,
    JiraUsername,
    JiraPassword,
    IsActive
)
VALUES
(
    1,
    'CRM',
    'CRM',
    'CRM',
    'jira-crm-user',
    'change-me',
    1
);

DROP TABLE IF EXISTS temp._SeedIssueTypes;
CREATE TEMP TABLE _SeedIssueTypes
(
    IssueTypeCode TEXT PRIMARY KEY,
    JiraIssueTypeName TEXT NOT NULL,
    IsActive INTEGER NOT NULL DEFAULT 1 CHECK (IsActive IN (0, 1))
);

INSERT INTO _SeedIssueTypes (IssueTypeCode, JiraIssueTypeName, IsActive)
VALUES ('BUG', 'Bug', 1);

INSERT INTO _SeedIssueTypes (IssueTypeCode, JiraIssueTypeName, IsActive)
VALUES ('TASK', 'Task', 1);

-- Optional custom field mappings.
-- For product-level fallback fields, set IssueTypeCode to NULL.
DROP TABLE IF EXISTS temp._SeedFieldMappings;
CREATE TEMP TABLE _SeedFieldMappings
(
    IssueTypeCode TEXT NULL,
    SourceField TEXT NOT NULL,
    JiraField TEXT NOT NULL,
    IsRequired INTEGER NOT NULL DEFAULT 0 CHECK (IsRequired IN (0, 1)),
    DefaultValue TEXT NULL,
    IsActive INTEGER NOT NULL DEFAULT 1 CHECK (IsActive IN (0, 1))
);

INSERT INTO _SeedFieldMappings
(
    IssueTypeCode,
    SourceField,
    JiraField,
    IsRequired,
    DefaultValue,
    IsActive
)
VALUES ('BUG', 'customerId', 'customfield_10010', 0, NULL, 1);

INSERT INTO _SeedFieldMappings
(
    IssueTypeCode,
    SourceField,
    JiraField,
    IsRequired,
    DefaultValue,
    IsActive
)
VALUES ('BUG', 'sourceRecordId', 'customfield_10011', 0, NULL, 1);

-- Status mappings.
-- For update status API, JiraTransitionId or JiraTransitionName should be filled.
-- For get status API, JiraStatusName is used to map Jira status back to StandardStatus.
DROP TABLE IF EXISTS temp._SeedStatusMappings;
CREATE TEMP TABLE _SeedStatusMappings
(
    IssueTypeCode TEXT NULL,
    StandardStatus TEXT NOT NULL CHECK (StandardStatus IN ('OPEN', 'IN_PROGRESS', 'WAITING', 'DONE', 'CANCELLED')),
    JiraStatusName TEXT NOT NULL,
    JiraTransitionId TEXT NULL,
    JiraTransitionName TEXT NULL,
    IsActive INTEGER NOT NULL DEFAULT 1 CHECK (IsActive IN (0, 1))
);

INSERT INTO _SeedStatusMappings
(
    IssueTypeCode,
    StandardStatus,
    JiraStatusName,
    JiraTransitionId,
    JiraTransitionName,
    IsActive
)
VALUES ('BUG', 'OPEN', 'To Do', NULL, NULL, 1);

INSERT INTO _SeedStatusMappings
(
    IssueTypeCode,
    StandardStatus,
    JiraStatusName,
    JiraTransitionId,
    JiraTransitionName,
    IsActive
)
VALUES ('BUG', 'IN_PROGRESS', 'In Progress', '31', 'Start Progress', 1);

INSERT INTO _SeedStatusMappings
(
    IssueTypeCode,
    StandardStatus,
    JiraStatusName,
    JiraTransitionId,
    JiraTransitionName,
    IsActive
)
VALUES ('BUG', 'WAITING', 'Waiting', '41', 'Waiting', 1);

INSERT INTO _SeedStatusMappings
(
    IssueTypeCode,
    StandardStatus,
    JiraStatusName,
    JiraTransitionId,
    JiraTransitionName,
    IsActive
)
VALUES ('BUG', 'DONE', 'Done', '51', 'Done', 1);

INSERT INTO _SeedStatusMappings
(
    IssueTypeCode,
    StandardStatus,
    JiraStatusName,
    JiraTransitionId,
    JiraTransitionName,
    IsActive
)
VALUES ('BUG', 'CANCELLED', 'Cancelled', '61', 'Cancel', 1);

-- Example product-level fallback status mapping.
-- Uncomment and edit if an issue type should inherit this mapping.
--
-- INSERT INTO _SeedStatusMappings
-- (
--     IssueTypeCode,
--     StandardStatus,
--     JiraStatusName,
--     JiraTransitionId,
--     JiraTransitionName,
--     IsActive
-- )
-- VALUES (NULL, 'DONE', 'Closed', '51', 'Close', 1);

-- ============================================================
-- APPLY SECTION - normally no need to edit below this line.
-- ============================================================

UPDATE _SeedProduct
SET ProductCode = UPPER(TRIM(ProductCode)),
    ProductName = TRIM(ProductName),
    JiraProjectKey = UPPER(TRIM(JiraProjectKey)),
    JiraUsername = TRIM(JiraUsername),
    JiraPassword = TRIM(JiraPassword);

UPDATE _SeedIssueTypes
SET IssueTypeCode = UPPER(TRIM(IssueTypeCode)),
    JiraIssueTypeName = TRIM(JiraIssueTypeName);

UPDATE _SeedFieldMappings
SET IssueTypeCode = CASE
        WHEN IssueTypeCode IS NULL OR TRIM(IssueTypeCode) = '' THEN NULL
        ELSE UPPER(TRIM(IssueTypeCode))
    END,
    SourceField = TRIM(SourceField),
    JiraField = TRIM(JiraField),
    DefaultValue = NULLIF(TRIM(DefaultValue), '');

UPDATE _SeedStatusMappings
SET IssueTypeCode = CASE
        WHEN IssueTypeCode IS NULL OR TRIM(IssueTypeCode) = '' THEN NULL
        ELSE UPPER(TRIM(IssueTypeCode))
    END,
    StandardStatus = UPPER(TRIM(StandardStatus)),
    JiraStatusName = TRIM(JiraStatusName),
    JiraTransitionId = NULLIF(TRIM(JiraTransitionId), ''),
    JiraTransitionName = NULLIF(TRIM(JiraTransitionName), '');

INSERT INTO Products
(
    Code,
    Name,
    JiraProjectKey,
    IsActive,
    CreatedAt,
    UpdatedAt
)
SELECT
    ProductCode,
    ProductName,
    JiraProjectKey,
    IsActive,
    datetime('now'),
    datetime('now')
FROM _SeedProduct seed
WHERE NOT EXISTS
(
    SELECT 1
    FROM Products product
    WHERE product.Code = seed.ProductCode
);

UPDATE Products
SET Name = (SELECT ProductName FROM _SeedProduct WHERE Id = 1),
    JiraProjectKey = (SELECT JiraProjectKey FROM _SeedProduct WHERE Id = 1),
    IsActive = (SELECT IsActive FROM _SeedProduct WHERE Id = 1),
    UpdatedAt = datetime('now')
WHERE Code = (SELECT ProductCode FROM _SeedProduct WHERE Id = 1);

UPDATE JiraCredentials
SET Username = (SELECT JiraUsername FROM _SeedProduct WHERE Id = 1),
    Password = (SELECT JiraPassword FROM _SeedProduct WHERE Id = 1),
    IsActive = 1,
    UpdatedAt = datetime('now')
WHERE ProductId =
(
    SELECT product.Id
    FROM Products product
    JOIN _SeedProduct seed ON seed.ProductCode = product.Code
)
AND IsActive = 1;

INSERT INTO JiraCredentials
(
    ProductId,
    Username,
    Password,
    IsActive,
    CreatedAt,
    UpdatedAt
)
SELECT
    product.Id,
    seed.JiraUsername,
    seed.JiraPassword,
    1,
    datetime('now'),
    datetime('now')
FROM Products product
JOIN _SeedProduct seed ON seed.ProductCode = product.Code
WHERE NOT EXISTS
(
    SELECT 1
    FROM JiraCredentials credential
    WHERE credential.ProductId = product.Id
      AND credential.IsActive = 1
);

UPDATE IssueTypeMappings
SET JiraIssueTypeName =
    (
        SELECT seedIssueType.JiraIssueTypeName
        FROM _SeedIssueTypes seedIssueType
        WHERE seedIssueType.IssueTypeCode = IssueTypeMappings.IssueTypeCode
    ),
    IsActive =
    (
        SELECT seedIssueType.IsActive
        FROM _SeedIssueTypes seedIssueType
        WHERE seedIssueType.IssueTypeCode = IssueTypeMappings.IssueTypeCode
    ),
    UpdatedAt = datetime('now')
WHERE ProductId =
(
    SELECT product.Id
    FROM Products product
    JOIN _SeedProduct seed ON seed.ProductCode = product.Code
)
AND IssueTypeCode IN
(
    SELECT IssueTypeCode
    FROM _SeedIssueTypes
);

INSERT INTO IssueTypeMappings
(
    ProductId,
    IssueTypeCode,
    JiraIssueTypeName,
    IsActive,
    CreatedAt,
    UpdatedAt
)
SELECT
    product.Id,
    seedIssueType.IssueTypeCode,
    seedIssueType.JiraIssueTypeName,
    seedIssueType.IsActive,
    datetime('now'),
    datetime('now')
FROM _SeedIssueTypes seedIssueType
CROSS JOIN _SeedProduct seed
JOIN Products product ON product.Code = seed.ProductCode
WHERE NOT EXISTS
(
    SELECT 1
    FROM IssueTypeMappings issueType
    WHERE issueType.ProductId = product.Id
      AND issueType.IssueTypeCode = seedIssueType.IssueTypeCode
);

DELETE FROM FieldMappings
WHERE ProductId =
(
    SELECT product.Id
    FROM Products product
    JOIN _SeedProduct seed ON seed.ProductCode = product.Code
)
AND IssueTypeMappingId IS NULL
AND SourceField IN
(
    SELECT SourceField
    FROM _SeedFieldMappings
    WHERE IssueTypeCode IS NULL
);

DELETE FROM FieldMappings
WHERE ProductId =
(
    SELECT product.Id
    FROM Products product
    JOIN _SeedProduct seed ON seed.ProductCode = product.Code
)
AND EXISTS
(
    SELECT 1
    FROM _SeedFieldMappings seedField
    JOIN IssueTypeMappings issueType
      ON issueType.ProductId = FieldMappings.ProductId
     AND issueType.IssueTypeCode = seedField.IssueTypeCode
    WHERE seedField.IssueTypeCode IS NOT NULL
      AND FieldMappings.IssueTypeMappingId = issueType.Id
      AND FieldMappings.SourceField = seedField.SourceField
);

INSERT INTO FieldMappings
(
    ProductId,
    IssueTypeMappingId,
    SourceField,
    JiraField,
    IsRequired,
    DefaultValue,
    IsActive
)
SELECT
    product.Id,
    issueType.Id,
    seedField.SourceField,
    seedField.JiraField,
    seedField.IsRequired,
    seedField.DefaultValue,
    seedField.IsActive
FROM _SeedFieldMappings seedField
CROSS JOIN _SeedProduct seed
JOIN Products product ON product.Code = seed.ProductCode
LEFT JOIN IssueTypeMappings issueType
  ON issueType.ProductId = product.Id
 AND issueType.IssueTypeCode = seedField.IssueTypeCode
WHERE seedField.IssueTypeCode IS NULL
   OR issueType.Id IS NOT NULL;

DELETE FROM StatusMappings
WHERE ProductId =
(
    SELECT product.Id
    FROM Products product
    JOIN _SeedProduct seed ON seed.ProductCode = product.Code
)
AND IssueTypeMappingId IS NULL
AND StandardStatus IN
(
    SELECT StandardStatus
    FROM _SeedStatusMappings
    WHERE IssueTypeCode IS NULL
);

DELETE FROM StatusMappings
WHERE ProductId =
(
    SELECT product.Id
    FROM Products product
    JOIN _SeedProduct seed ON seed.ProductCode = product.Code
)
AND EXISTS
(
    SELECT 1
    FROM _SeedStatusMappings seedStatus
    JOIN IssueTypeMappings issueType
      ON issueType.ProductId = StatusMappings.ProductId
     AND issueType.IssueTypeCode = seedStatus.IssueTypeCode
    WHERE seedStatus.IssueTypeCode IS NOT NULL
      AND StatusMappings.IssueTypeMappingId = issueType.Id
      AND StatusMappings.StandardStatus = seedStatus.StandardStatus
);

INSERT INTO StatusMappings
(
    ProductId,
    IssueTypeMappingId,
    StandardStatus,
    JiraStatusName,
    JiraTransitionId,
    JiraTransitionName,
    IsActive
)
SELECT
    product.Id,
    issueType.Id,
    seedStatus.StandardStatus,
    seedStatus.JiraStatusName,
    seedStatus.JiraTransitionId,
    seedStatus.JiraTransitionName,
    seedStatus.IsActive
FROM _SeedStatusMappings seedStatus
CROSS JOIN _SeedProduct seed
JOIN Products product ON product.Code = seed.ProductCode
LEFT JOIN IssueTypeMappings issueType
  ON issueType.ProductId = product.Id
 AND issueType.IssueTypeCode = seedStatus.IssueTypeCode
WHERE seedStatus.IssueTypeCode IS NULL
   OR issueType.Id IS NOT NULL;

COMMIT;

-- Verification output. Password is not printed.
SELECT
    'Product' AS Section,
    product.Id,
    product.Code,
    product.Name,
    product.JiraProjectKey,
    product.IsActive
FROM Products product
JOIN _SeedProduct seed ON seed.ProductCode = product.Code;

SELECT
    'JiraCredential' AS Section,
    credential.Id,
    product.Code AS ProductCode,
    credential.Username,
    length(credential.Password) AS PasswordLength,
    credential.IsActive
FROM JiraCredentials credential
JOIN Products product ON product.Id = credential.ProductId
JOIN _SeedProduct seed ON seed.ProductCode = product.Code
WHERE credential.IsActive = 1;

SELECT
    'IssueType' AS Section,
    issueType.Id,
    product.Code AS ProductCode,
    issueType.IssueTypeCode,
    issueType.JiraIssueTypeName,
    issueType.IsActive
FROM IssueTypeMappings issueType
JOIN Products product ON product.Id = issueType.ProductId
JOIN _SeedProduct seed ON seed.ProductCode = product.Code
ORDER BY issueType.IssueTypeCode;

SELECT
    'FieldMapping' AS Section,
    fieldMapping.Id,
    product.Code AS ProductCode,
    issueType.IssueTypeCode,
    fieldMapping.SourceField,
    fieldMapping.JiraField,
    fieldMapping.IsRequired,
    fieldMapping.DefaultValue,
    fieldMapping.IsActive
FROM FieldMappings fieldMapping
JOIN Products product ON product.Id = fieldMapping.ProductId
JOIN _SeedProduct seed ON seed.ProductCode = product.Code
LEFT JOIN IssueTypeMappings issueType ON issueType.Id = fieldMapping.IssueTypeMappingId
ORDER BY issueType.IssueTypeCode, fieldMapping.SourceField;

SELECT
    'StatusMapping' AS Section,
    statusMapping.Id,
    product.Code AS ProductCode,
    issueType.IssueTypeCode,
    statusMapping.StandardStatus,
    statusMapping.JiraStatusName,
    statusMapping.JiraTransitionId,
    statusMapping.JiraTransitionName,
    statusMapping.IsActive
FROM StatusMappings statusMapping
JOIN Products product ON product.Id = statusMapping.ProductId
JOIN _SeedProduct seed ON seed.ProductCode = product.Code
LEFT JOIN IssueTypeMappings issueType ON issueType.Id = statusMapping.IssueTypeMappingId
ORDER BY issueType.IssueTypeCode, statusMapping.StandardStatus;
