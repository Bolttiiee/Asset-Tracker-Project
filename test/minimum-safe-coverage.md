# Minimum Safe Coverage For This Sprint

## Goal
Cover the highest-risk frontend logic that can break authentication, inventory operations, and admin workflows without trying to exhaustively test every component this sprint.

## Definition Of Done
- Critical auth flows have unit coverage.
- Inventory mutation flows have unit coverage.
- Registration validation has unit coverage.
- Admin user actions have unit coverage.
- Core service request/response shaping is covered.
- Test suite stays green after adding new specs.

## Sprint Coverage List

### 1. Auth Service
File: src/app/services/auth.ts

Add tests for:
- login stores `token`, `asset_tracker_token`, `user_profile`, and `userRole` when response contains `token` and `user`.
- login stores `userRole` from top-level `role` when `response.user.role` is missing.
- logout clears all auth-related local storage keys.
- isLoggedIn returns true when either `token` or `asset_tracker_token` exists.
- forgotPassword posts `{ email }` to `/forgot-password`.
- resetPassword posts `{ token, newPassword }` to `/reset-password`.

### 2. Login Component
File: src/app/pages/login/login.ts

Add tests for:
- invalid form prevents login submission.
- successful login navigates to `/inventory`.
- failed login sets `errorMessage`.
- forgot-password panel opens when trigger is clicked.
- forgot-password invalid email prevents API call.
- forgot-password success sets confirmation message.
- forgot-password failure sets error message.

### 3. Reset Password Component
File: src/app/components/reset-password/reset-password.ts

Add tests for:
- token is read from query params on init.
- missing token sets security error state.
- invalid password blocks submit.
- successful reset sets success message.
- failed reset shows backend error or fallback message.

### 4. Inventory Grid Component
File: src/app/components/inventory-grid/inventory-grid.ts

Add tests for:
- filteredAssets filters correctly by search term.
- filteredAssets filters correctly by status.
- filteredAssets filters correctly by minimum value.
- isAdminOrManager returns true only for `Admin` and `Asset Manager`.
- checkout is blocked for missing required fields.
- checkout is blocked for invalid email.
- checkout is blocked for past return date.
- checkout is blocked for non-available or decommissioned assets.
- successful checkout updates local asset state before refresh.
- successful return sets asset status back to `Available`.
- maintenance toggle updates local asset status correctly.
- decommission and restore flows update local asset state correctly.

### 5. Asset Service
File: src/app/services/asset.ts

Add tests for:
- auth headers include bearer token from local storage.
- getAssets calls the assets endpoint.
- assignAsset posts the correct payload.
- returnAsset calls the correct return endpoint.
- adminOverrideReturn calls the correct endpoint.
- restoreAsset calls the correct endpoint.
- setMaintenanceStatus sends `{ setMaintenance }` correctly.
- getAuditTrail omits `action` when value is `All`.
- getAuditTrail includes `fromDate` and `toDate` when provided.

### 6. Register Component
File: src/app/pages/register/register.ts

Add tests for:
- password complexity validator rejects missing uppercase.
- password complexity validator rejects missing number.
- password complexity validator rejects missing special character.
- passwordMatchValidator rejects mismatched passwords.
- getPasswordStrength returns weak, medium, and strong states correctly.
- onSubmit does not call register when form is invalid.
- successful register sets success state/message.
- failed register sets error message.

### 7. User Administration Component
File: src/app/pages/user-administration/user-administration.ts

Add tests for:
- initial load sets `Pending` filter and loads users.
- loadUsers populates users on success.
- loadUsers sets error message on failure.
- approve flow calls approveUser service after confirmation.
- reject flow requires a non-empty reason.
- reject flow calls rejectUser service with reason.
- activation toggle calls updateUserStatus with correct boolean.
- role change calls updateUserRole with selected role.
- reset password calls resetUserPassword and shows returned temporary password.
- busy state resets on both success and failure paths.

### 8. User Service
File: src/app/services/user.ts

Add tests for:
- getAllUsers builds query string correctly for search, department, role, and status.
- getUserById calls the correct endpoint.
- approveUser posts to `/approve`.
- rejectUser posts rejection reason payload.
- updateUserStatus sends `{ activate }` payload.
- updateUserRole sends `{ newRole }` payload.
- resetUserPassword posts to `/reset-password`.

## Nice To Have If Time Remains
- Reports component search/export state tests.
- Report service query-param tests.
- My Assets component overdue and return flow tests.
- Audit Timeline filter behavior tests.
- Asset Modal duplicate serial handling tests.

## Suggested Execution Order
1. Auth service
2. Login component
3. Reset password component
4. Asset service
5. Inventory grid component
6. Register component
7. User service
8. User administration component

## Expected Sprint Outcome
If the items above are covered, the frontend will have safe unit protection around:
- authentication
- password recovery
- inventory state mutations
- registration validation
- admin approvals and user management
- service request contracts
