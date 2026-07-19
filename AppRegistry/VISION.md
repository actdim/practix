# Vision

## Overview

This project is a reusable .NET foundation for building organization-based applications with PostgreSQL as the primary storage engine.

The goal is to provide a common infrastructure for:

* users
* organizations
* projects
* roles
* permissions
* access control
* database schema isolation

The system is designed around a simple principle:

> One application owns one database. Organizations and projects are isolated inside PostgreSQL schemas.

---

# Architecture Overview

Each application has its own PostgreSQL database.

Example:

```
CRM Application
└── crm_database

ERP Application
└── erp_database
```

Applications do not share users, organizations, or permissions.

Inside the application database there are:

1. System schema
2. Project schemas

Example:

```
crm
│
├── users
├── organizations
├── projects
├── roles
├── permissions
├── org_users
├── org_roles
│
├── acme__backend
│   ├── tasks
│   ├── documents
│   └── events
│
└── acme__mobile
    ├── screens
    └── builds
```

---

# System Schema

The system schema contains application-level entities.

The schema name is configurable.

Default:

```
SystemSchema = ApplicationSlug
```

Example:

```
ApplicationSlug = crm

crm
├── users
├── organizations
└── projects
```

Custom configuration:

```
ApplicationSlug = crm
SystemSchema = platform
```

Result:

```
platform
├── users
├── organizations
└── projects
```

---

# Users

Users represent people who can access the application.

Table:

```
users
```

Structure:

```
id

username
email

given_name
middle_name
family_name
display_name

created_at
updated_at
```

## Identity

The user has:

* internal identifier (`id`)
* public identifier (`username`)

A separate `slug` field is not required.

Example:

```
username = ivan.petrov

given_name = Ivan
middle_name = Ivanovich
family_name = Petrov
```

---

# Organizations

Organizations represent isolated groups of users.

Table:

```
organizations
```

Structure:

```
id

slug
name

created_at
updated_at
```

Example:

```
slug = acme
name = ACME Corporation
```

The `slug` is used as a stable technical identifier.

---

# Organization Users

Defines membership between users and organizations.

Table:

```
org_users
```

Structure:

```
organization_id
user_id

created_at
```

A user may belong to multiple organizations.

Example:

```
Ivan

 ├── ACME
 └── Example Corp
```

---

# Roles

Roles define reusable access groups.

Table:

```
roles
```

Structure:

```
id

name
description

created_at
updated_at
```

Examples:

```
Owner
Admin
Developer
Viewer
```

---

# Permissions

Permissions define available actions.

Table:

```
permissions
```

Structure:

```
id

name
description
```

Examples:

```
project.read
project.write
project.delete
```

---

# Organization Roles

Defines user roles inside organizations.

Table:

```
org_roles
```

Structure:

```
organization_id
user_id
role_id
```

A user can have different roles in different organizations.

Example:

```
Ivan

ACME:
    Admin

Example Corp:
    Viewer
```

---

# Projects

Projects belong to organizations.

Table:

```
projects
```

Structure:

```
id

organization_id

slug
name

schema_name

created_at
updated_at
```

Example:

Organization:

```
slug = acme
```

Project:

```
slug = backend
```

Generated schema:

```
acme__backend
```

---

# Project Schemas

Each project receives its own PostgreSQL schema.

Example:

```
acme__backend

tasks
documents
events
audit_logs
```

Project data never mixes with other projects.

---

# Search Path

Project access uses PostgreSQL `search_path`.

Example:

```sql
SET search_path TO acme__backend, crm;
```

After that:

```sql
SELECT * FROM tasks;
```

resolves to:

```
acme__backend.tasks
```

and:

```sql
SELECT * FROM users;
```

resolves to:

```
crm.users
```

The application code does not need to know the physical schema names.

---

# Migrations

The system supports migrations for:

## System schema

Example:

```
crm

users
organizations
projects
roles
permissions
```

## Project schemas

When creating a project:

1. Create PostgreSQL schema

```sql
CREATE SCHEMA acme__backend;
```

2. Apply project migrations

Result:

```
acme__backend

tasks
documents
events
```

---

# Naming Rules

## Tables

Tables use plural names.

Examples:

```
users
organizations
projects
roles
permissions
```

Relations:

```
org_users
org_roles
role_permissions
```

---

## Identifiers

Internal relations use IDs.

Examples:

```
user_id
organization_id
project_id
```

Human-readable identifiers:

```
username
slug
name
```

are used for display and external references.

---

# Design Principles

## Simplicity

Avoid unnecessary abstraction.

The system does not introduce:

* tenants
* shared application databases
* cross-application identity

Each application owns its own database.

---

## Isolation

Organization projects are isolated using PostgreSQL schemas.

Project data and system data are separated.

---

## Configurability

The framework should not depend on fixed schema names.

The system schema is configurable.

Default behavior should work without additional configuration.

---

# Future Extensions

Possible future modules:

* authentication providers
* invitations
* audit logs
* API keys
* external identity providers
* project-level permissions
* role inheritance

Future extensions should preserve the current model:

```
User
 |
Organization
 |
Project
 |
Schema
```
