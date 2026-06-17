-- Insert initial configuration data for company/product EAS.
--
-- Run from the repository root:
-- sqlite3 src/JiraIntegrationService.Api/jira-integration.db ".read scripts/insert-crm-company.sql"
--
-- Assumption:
-- - This script clears existing configuration data before inserting EAS data.
-- - Replace Jira username/password, customfield ids, and transition ids with real Jira values before using.

PRAGMA foreign_keys = ON;

BEGIN TRANSACTION;

DELETE FROM FieldMappings;
DELETE FROM JiraCredentials;
DELETE FROM StatusMappings;
DELETE FROM IssueTypeMappings;
DELETE FROM Products;

DELETE FROM sqlite_sequence
WHERE name IN
(
    'Products',
    'JiraCredentials',
    'IssueTypeMappings',
    'FieldMappings',
    'StatusMappings'
);

INSERT INTO Products
(
    Code,
    Name,
    JiraProjectKey,
    IsActive,
    CreatedAt,
    UpdatedAt
)
VALUES
(
    'EAS',
    'EAS Company',
    'EAS',
    1,
    datetime('now'),
    datetime('now')
);

INSERT INTO JiraCredentials
(
    ProductId,
    Username,
    Password,
    IsActive,
    CreatedAt,
    UpdatedAt
)
VALUES
(
    (SELECT Id FROM Products WHERE Code = 'EAS'),
    'anh.phamviet',
    '123456Aa@',
    1,
    datetime('now'),
    datetime('now')
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
VALUES
(
    (SELECT Id FROM Products WHERE Code = 'EAS'),
    'BUG',
    'Bug',
    1,
    datetime('now'),
    datetime('now')
),
(
    (SELECT Id FROM Products WHERE Code = 'EAS'),
    'TASK',
    'Task',
    1,
    datetime('now'),
    datetime('now')
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
VALUES
(
    (SELECT Id FROM Products WHERE Code = 'EAS'),
    (
        SELECT Id
        FROM IssueTypeMappings
        WHERE ProductId = (SELECT Id FROM Products WHERE Code = 'EAS')
          AND IssueTypeCode = 'BUG'
    ),
    'customerId',
    'customfield_10010',
    0,
    NULL,
    1
),
(
    (SELECT Id FROM Products WHERE Code = 'EAS'),
    (
        SELECT Id
        FROM IssueTypeMappings
        WHERE ProductId = (SELECT Id FROM Products WHERE Code = 'EAS')
          AND IssueTypeCode = 'BUG'
    ),
    'sourceRecordId',
    'customfield_10011',
    0,
    NULL,
    1
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
VALUES
(
    (SELECT Id FROM Products WHERE Code = 'EAS'),
    (
        SELECT Id
        FROM IssueTypeMappings
        WHERE ProductId = (SELECT Id FROM Products WHERE Code = 'EAS')
          AND IssueTypeCode = 'BUG'
    ),
    'OPEN',
    'To Do',
    NULL,
    NULL,
    1
),
(
    (SELECT Id FROM Products WHERE Code = 'EAS'),
    (
        SELECT Id
        FROM IssueTypeMappings
        WHERE ProductId = (SELECT Id FROM Products WHERE Code = 'EAS')
          AND IssueTypeCode = 'BUG'
    ),
    'IN_PROGRESS',
    'In Progress',
    '31',
    'Start Progress',
    1
),
(
    (SELECT Id FROM Products WHERE Code = 'EAS'),
    (
        SELECT Id
        FROM IssueTypeMappings
        WHERE ProductId = (SELECT Id FROM Products WHERE Code = 'EAS')
          AND IssueTypeCode = 'BUG'
    ),
    'WAITING',
    'Waiting',
    '41',
    'Waiting',
    1
),
(
    (SELECT Id FROM Products WHERE Code = 'EAS'),
    (
        SELECT Id
        FROM IssueTypeMappings
        WHERE ProductId = (SELECT Id FROM Products WHERE Code = 'EAS')
          AND IssueTypeCode = 'BUG'
    ),
    'DONE',
    'Done',
    '51',
    'Done',
    1
),
(
    (SELECT Id FROM Products WHERE Code = 'EAS'),
    (
        SELECT Id
        FROM IssueTypeMappings
        WHERE ProductId = (SELECT Id FROM Products WHERE Code = 'EAS')
          AND IssueTypeCode = 'BUG'
    ),
    'CANCELLED',
    'Cancelled',
    '61',
    'Cancel',
    1
);

COMMIT;
