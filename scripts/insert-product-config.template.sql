-- Jira Integration Service - default configuration seed for SQLite.
--
-- This file replaces the old EF Core HasData seeding. The migrations now only
-- create the schema; ALL default data lives here so you can edit it freely
-- without ever adding a new migration.
--
-- Usage (from repo root), after the database schema has been created:
--   sqlite3 src/JiraIntegrationService.Api/jira-integration.db ".read scripts/insert-product-config.template.sql"
--
-- Typical flow when recreating the DB:
--   1. delete src/JiraIntegrationService.Api/jira-integration.db
--   2. dotnet run            (migrations create the empty schema)
--   3. run this script       (loads the default configuration)
--
-- Notes:
--   - The script is idempotent: it UPSERTs rows, so running it again just
--     updates existing values instead of creating duplicates.
--   - Codes are stored in upper case (Product Code, IssueTypeCode, StandardStatus).
--   - To change any default value, edit it directly below and re-run the script.

PRAGMA foreign_keys = ON;

BEGIN TRANSACTION;

-- ============================================================
-- 1) PRODUCT
--    Unique key: Code
-- ============================================================
INSERT INTO Products
    (Code, Name, JiraProjectKey, JiraBaseUrl, JiraApiBasePath, JiraVersion, IsActive, CreatedAt, UpdatedAt)
VALUES
    ('EAS', 'EAS', 'EAS', 'https://jira.ezcloudhotel.com', '/rest/api/2', 'ServerV2', 1, datetime('now'), datetime('now'))
ON CONFLICT(Code) DO UPDATE SET
    Name            = excluded.Name,
    JiraProjectKey  = excluded.JiraProjectKey,
    JiraBaseUrl     = excluded.JiraBaseUrl,
    JiraApiBasePath = excluded.JiraApiBasePath,
    JiraVersion     = excluded.JiraVersion,
    IsActive        = excluded.IsActive,
    UpdatedAt       = datetime('now');

-- ============================================================
-- 2) JIRA CREDENTIAL (Basic auth)
--    No unique key -> replace the product's credentials.
-- ============================================================
DELETE FROM JiraCredentials
WHERE ProductId = (SELECT Id FROM Products WHERE Code = 'EAS');

INSERT INTO JiraCredentials
    (ProductId, AuthType, Username, PasswordOrToken, IsActive, CreatedAt, UpdatedAt)
VALUES
    ((SELECT Id FROM Products WHERE Code = 'EAS'), 'Basic', 'anh.phamviet', '123456Aa@', 1, datetime('now'), datetime('now'));

-- ============================================================
-- 3) ISSUE TYPE MAPPINGS
--    Unique key: (ProductId, IssueTypeCode)
-- ============================================================
INSERT INTO IssueTypeMappings
    (ProductId, IssueTypeCode, JiraIssueTypeId, JiraIssueTypeName, IsActive, CreatedAt, UpdatedAt)
VALUES
    ((SELECT Id FROM Products WHERE Code = 'EAS'), 'BUG',  NULL, 'Bug',  1, datetime('now'), datetime('now')),
    ((SELECT Id FROM Products WHERE Code = 'EAS'), 'TASK', NULL, 'Task', 1, datetime('now'), datetime('now'))
ON CONFLICT(ProductId, IssueTypeCode) DO UPDATE SET
    JiraIssueTypeId   = excluded.JiraIssueTypeId,
    JiraIssueTypeName = excluded.JiraIssueTypeName,
    IsActive          = excluded.IsActive,
    UpdatedAt         = datetime('now');

-- ============================================================
-- 4) FIELD MAPPING TEMPLATES (one DEFAULT template per issue type)
--    Unique key: (ProductId, IssueTypeMappingId, TemplateCode)
-- ============================================================
INSERT INTO IssueFieldMappingTemplates
    (ProductId, IssueTypeMappingId, TemplateCode, Name, Description, IsDefault, IsActive, CreatedAt, UpdatedAt)
VALUES
    ((SELECT Id FROM Products WHERE Code = 'EAS'),
     (SELECT Id FROM IssueTypeMappings WHERE ProductId = (SELECT Id FROM Products WHERE Code = 'EAS') AND IssueTypeCode = 'BUG'),
     'DEFAULT', 'Default', 'Default field mapping template.', 1, 1, datetime('now'), datetime('now')),
    ((SELECT Id FROM Products WHERE Code = 'EAS'),
     (SELECT Id FROM IssueTypeMappings WHERE ProductId = (SELECT Id FROM Products WHERE Code = 'EAS') AND IssueTypeCode = 'TASK'),
     'DEFAULT', 'Default', 'Default field mapping template.', 1, 1, datetime('now'), datetime('now'))
ON CONFLICT(ProductId, IssueTypeMappingId, TemplateCode) DO UPDATE SET
    Name        = excluded.Name,
    Description = excluded.Description,
    IsDefault   = excluded.IsDefault,
    IsActive    = excluded.IsActive,
    UpdatedAt   = datetime('now');

-- ============================================================
-- 5) FIELD MAPPINGS (DEFAULT template of the BUG issue type)
--    Unique key: (ProductId, IssueTypeMappingId, TemplateCode, SourcePath)
--    ValueShape: 'raw' | 'name' | ...   ValueType: 'string' | ...
-- ============================================================
INSERT INTO IssueFieldMappings
    (ProductId, IssueTypeMappingId, TemplateCode, SourcePath, JiraField, ValueType, ValueShape, IsRequired, DefaultValue, SortOrder, IsActive, TransformConfigJson, CreatedAt, UpdatedAt)
VALUES
    ((SELECT Id FROM Products WHERE Code = 'EAS'),
     (SELECT Id FROM IssueTypeMappings WHERE ProductId = (SELECT Id FROM Products WHERE Code = 'EAS') AND IssueTypeCode = 'BUG'),
     'DEFAULT', 'data.summary',       'summary',           'string', 'raw',  1, NULL, 10, 1, NULL, datetime('now'), datetime('now')),
    ((SELECT Id FROM Products WHERE Code = 'EAS'),
     (SELECT Id FROM IssueTypeMappings WHERE ProductId = (SELECT Id FROM Products WHERE Code = 'EAS') AND IssueTypeCode = 'BUG'),
     'DEFAULT', 'data.description',   'description',       'string', 'raw',  0, NULL, 20, 1, NULL, datetime('now'), datetime('now')),
    ((SELECT Id FROM Products WHERE Code = 'EAS'),
     (SELECT Id FROM IssueTypeMappings WHERE ProductId = (SELECT Id FROM Products WHERE Code = 'EAS') AND IssueTypeCode = 'BUG'),
     'DEFAULT', 'data.priority',      'priority',          'string', 'name', 0, NULL, 30, 1, NULL, datetime('now'), datetime('now')),
    ((SELECT Id FROM Products WHERE Code = 'EAS'),
     (SELECT Id FROM IssueTypeMappings WHERE ProductId = (SELECT Id FROM Products WHERE Code = 'EAS') AND IssueTypeCode = 'BUG'),
     'DEFAULT', 'data.customer.code', 'customfield_10010', 'string', 'raw',  0, NULL, 40, 1, NULL, datetime('now'), datetime('now')),
    ((SELECT Id FROM Products WHERE Code = 'EAS'),
     (SELECT Id FROM IssueTypeMappings WHERE ProductId = (SELECT Id FROM Products WHERE Code = 'EAS') AND IssueTypeCode = 'BUG'),
     'DEFAULT', 'data.ticket.url',    'customfield_10011', 'string', 'raw',  0, NULL, 50, 1, NULL, datetime('now'), datetime('now'))
ON CONFLICT(ProductId, IssueTypeMappingId, TemplateCode, SourcePath) DO UPDATE SET
    JiraField           = excluded.JiraField,
    ValueType           = excluded.ValueType,
    ValueShape          = excluded.ValueShape,
    IsRequired          = excluded.IsRequired,
    DefaultValue        = excluded.DefaultValue,
    SortOrder           = excluded.SortOrder,
    IsActive            = excluded.IsActive,
    TransformConfigJson = excluded.TransformConfigJson,
    UpdatedAt           = datetime('now');

-- ============================================================
-- 6) STATUS MAPPINGS (BUG issue type)
--    Unique key: (ProductId, IssueTypeMappingId, StandardStatus, JiraStatusName)
--    StandardStatus in: OPEN | IN_PROGRESS | WAITING | DONE | CANCELLED
-- ============================================================
INSERT INTO StatusMappings
    (ProductId, IssueTypeMappingId, StandardStatus, JiraStatusName, JiraTransitionId, JiraTransitionName, IsActive)
VALUES
    ((SELECT Id FROM Products WHERE Code = 'EAS'),
     (SELECT Id FROM IssueTypeMappings WHERE ProductId = (SELECT Id FROM Products WHERE Code = 'EAS') AND IssueTypeCode = 'BUG'),
     'OPEN',        'To Do',       NULL, NULL,             1),
    ((SELECT Id FROM Products WHERE Code = 'EAS'),
     (SELECT Id FROM IssueTypeMappings WHERE ProductId = (SELECT Id FROM Products WHERE Code = 'EAS') AND IssueTypeCode = 'BUG'),
     'IN_PROGRESS', 'In Progress', '31', 'Start Progress', 1),
    ((SELECT Id FROM Products WHERE Code = 'EAS'),
     (SELECT Id FROM IssueTypeMappings WHERE ProductId = (SELECT Id FROM Products WHERE Code = 'EAS') AND IssueTypeCode = 'BUG'),
     'WAITING',     'Waiting',     '41', 'Waiting',        1),
    ((SELECT Id FROM Products WHERE Code = 'EAS'),
     (SELECT Id FROM IssueTypeMappings WHERE ProductId = (SELECT Id FROM Products WHERE Code = 'EAS') AND IssueTypeCode = 'BUG'),
     'DONE',        'Done',        '51', 'Done',           1),
    ((SELECT Id FROM Products WHERE Code = 'EAS'),
     (SELECT Id FROM IssueTypeMappings WHERE ProductId = (SELECT Id FROM Products WHERE Code = 'EAS') AND IssueTypeCode = 'BUG'),
     'CANCELLED',   'Cancelled',   '61', 'Cancel',         1)
ON CONFLICT(ProductId, IssueTypeMappingId, StandardStatus, JiraStatusName) DO UPDATE SET
    JiraTransitionId   = excluded.JiraTransitionId,
    JiraTransitionName = excluded.JiraTransitionName,
    IsActive           = excluded.IsActive;

COMMIT;

-- ============================================================
-- VERIFICATION (password length only, not the value)
-- ============================================================
SELECT 'Product' AS Section, Id, Code, Name, JiraProjectKey, JiraBaseUrl, JiraVersion, IsActive
FROM Products WHERE Code = 'EAS';

SELECT 'JiraCredential' AS Section, c.Id, p.Code AS ProductCode, c.AuthType, c.Username, length(c.PasswordOrToken) AS PasswordLength, c.IsActive
FROM JiraCredentials c JOIN Products p ON p.Id = c.ProductId WHERE p.Code = 'EAS';

SELECT 'IssueType' AS Section, it.Id, p.Code AS ProductCode, it.IssueTypeCode, it.JiraIssueTypeName, it.IsActive
FROM IssueTypeMappings it JOIN Products p ON p.Id = it.ProductId WHERE p.Code = 'EAS'
ORDER BY it.IssueTypeCode;

SELECT 'Template' AS Section, t.Id, p.Code AS ProductCode, it.IssueTypeCode, t.TemplateCode, t.Name, t.IsDefault, t.IsActive
FROM IssueFieldMappingTemplates t
JOIN Products p ON p.Id = t.ProductId
JOIN IssueTypeMappings it ON it.Id = t.IssueTypeMappingId
WHERE p.Code = 'EAS' ORDER BY it.IssueTypeCode, t.TemplateCode;

SELECT 'FieldMapping' AS Section, f.Id, p.Code AS ProductCode, it.IssueTypeCode, f.TemplateCode, f.SourcePath, f.JiraField, f.ValueType, f.ValueShape, f.IsRequired, f.SortOrder, f.IsActive
FROM IssueFieldMappings f
JOIN Products p ON p.Id = f.ProductId
LEFT JOIN IssueTypeMappings it ON it.Id = f.IssueTypeMappingId
WHERE p.Code = 'EAS' ORDER BY it.IssueTypeCode, f.SortOrder;

SELECT 'StatusMapping' AS Section, s.Id, p.Code AS ProductCode, it.IssueTypeCode, s.StandardStatus, s.JiraStatusName, s.JiraTransitionId, s.JiraTransitionName, s.IsActive
FROM StatusMappings s
JOIN Products p ON p.Id = s.ProductId
LEFT JOIN IssueTypeMappings it ON it.Id = s.IssueTypeMappingId
WHERE p.Code = 'EAS' ORDER BY it.IssueTypeCode, s.StandardStatus;
