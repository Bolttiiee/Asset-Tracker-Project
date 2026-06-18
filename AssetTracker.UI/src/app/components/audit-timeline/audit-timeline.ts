import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuditTrail } from '../../models/audit-trail.model';
import { AssetService } from '../../services/asset';

const ACTION_LABELS: Record<string, string> = {
  All: 'All Actions',
  PROVISION: 'Asset Provisioned',
  ASSIGN: 'Asset Assigned',
  RETURN: 'Asset Returned',
  DECOMMISSION: 'Asset Decommissioned',
  RESTORE: 'Asset Restored',
  SET_MAINTENANCE: 'Set Under Maintenance',
  CLEAR_MAINTENANCE: 'Cleared from Maintenance',
  APPROVE_USER: 'User Account Approved',
  REJECT_USER: 'User Account Rejected',
  CHANGE_USER_ROLE: 'User Role Changed',
  ACTIVATE_USER: 'User Account Activated',
  DEACTIVATE_USER: 'User Account Deactivated',
  RESET_PASSWORD: 'Password Reset',
  EMAIL_REMINDER: 'Overdue Reminder Email Sent',
  ADMIN_DIGEST: 'Admin Overdue Digest Sent',
  ADMIN_OVERRIDE_RETURN: 'Admin Force Return',
  CANCEL_CHECKOUT: 'Checkout Cancelled',
};

@Component({
  selector: 'app-audit-timeline',
  imports: [CommonModule, FormsModule],
  templateUrl: './audit-timeline.html',
  styleUrl: './audit-timeline.css',
})
export class AuditTimelineComponent implements OnInit {
  logs: AuditTrail[] = [];
  availableActions: string[] = [];
  selectedAction = 'All';
  fromDate = '';
  toDate = '';
  errorMessage = '';

  getActionLabel(action: string): string {
    return ACTION_LABELS[action] ?? action;
  }

  constructor(private assetService: AssetService, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.loadSecurityLogs('All', true);
  }

  loadSecurityLogs(action: string = 'All', refreshActions: boolean = false): void {
    this.assetService.getAuditTrail(action, this.fromDate, this.toDate).subscribe({
      next: (data) => {
        this.logs = data;

        if (refreshActions || this.availableActions.length === 0) {
          this.availableActions = ['All', ...new Set(data.map((log) => log.action).filter((item) => !!item))];
        }

        this.cdr.detectChanges();
      },
      error: (err) => {
        this.errorMessage = 'Access Denied: Insufficient authorization level to read security audit trails.';
        console.error(err);
        this.cdr.detectChanges();
      }
    });
  }

  onActionChange(): void {
    this.loadSecurityLogs(this.selectedAction);
  }

  onDateRangeChange(): void {
    this.loadSecurityLogs(this.selectedAction);
  }

  clearDateRange(): void {
    this.fromDate = '';
    this.toDate = '';
    this.loadSecurityLogs(this.selectedAction);
  }

  // Helper method to assign custom distinct icon accent background indicators based on operation category
  getActionColor(action: string): string {
    switch (action) {
      case 'PROVISION': return '#2b6cb0'; // Blue
      case 'ASSIGN': return '#319795'; // Teal
      case 'RETURN': return '#dd6b20'; // Orange
      case 'DECOMMISSION': return '#e53e3e'; // Red
      default: return '#718096';
    }
  }
}
