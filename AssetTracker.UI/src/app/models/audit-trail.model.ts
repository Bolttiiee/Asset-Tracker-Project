export interface AuditTrail {
  auditLogID: number;
  action:
    | 'PROVISION'
    | 'ASSIGN'
    | 'RETURN'
    | 'DECOMMISSION'
    | 'RESTORE'
    | 'SET_MAINTENANCE'
    | 'CLEAR_MAINTENANCE'
    | 'APPROVE_USER'
    | 'REJECT_USER'
    | 'CHANGE_USER_ROLE'
    | 'ACTIVATE_USER'
    | 'DEACTIVATE_USER'
    | 'RESET_PASSWORD'
    | 'EMAIL_REMINDER'
    | 'ADMIN_DIGEST'
    | 'ADMIN_OVERRIDE_RETURN'
    | 'CANCEL_CHECKOUT'
    | string; // forward-compatible: new backend action types won't break the UI
  assetID: number;
  assetName: string;
  performedByEmail: string;
  timestamp: string | Date;
  details: string;
}
