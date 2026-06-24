import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, HostListener, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ConfirmationDialogComponent } from '../../components/confirmation-dialog/confirmation-dialog';
import { UserDetails, UserService, UserSummary } from '../../services/user';

@Component({
  selector: 'app-user-administration',
  standalone: true,
  imports: [CommonModule, FormsModule, ConfirmationDialogComponent],
  templateUrl: './user-administration.html',
  styleUrls: ['./user-administration.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UserAdministrationComponent implements OnInit {
  users: UserSummary[] = [];
  selectedUser: UserDetails | null = null;
  loading = false;
  actionMessage = '';
  actionMessageType: 'success' | 'error' = 'success';
  errorMessage = '';
  expandedUserId: number | null = null;

  toggleExpand(id: number, event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (target.closest('.btn-action-trigger') || target.closest('.action-dropdown-menu') || target.closest('select') || target.closest('button') || target.closest('input')) {
      return;
    }
    const user = this.users.find(u => u.userID === id);
    if (!user || user.accountStatus !== 'Active') {
      return;
    }
    if (this.expandedUserId === id) {
      this.expandedUserId = null;
    } else {
      this.expandedUserId = id;
      this.openUser(id);
    }
  }
  
  openDropdownId: number | null = null;

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    this.openDropdownId = null;
  }

  toggleDropdown(id: number, event: MouseEvent): void {
    event.stopPropagation();
    if (this.openDropdownId === id) {
      this.openDropdownId = null;
    } else {
      this.openDropdownId = id;
    }
  }

  confirmationModalOpen = false;
  confirmationTitle = '';
  confirmationMessage = '';
  confirmationCancelLabel = 'Cancel';
  confirmationAction: (() => void) | null = null;

  // Inline rejection modal state
  rejectModalOpen = false;
  rejectTargetUser: UserSummary | null = null;
  rejectReason = '';

  // Per-user action loading tracking
  busyUserId: number | null = null;

  filters = {
    search: '',
    department: '',
    role: '',
    status: ''
  };

  constructor(private userService: UserService, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    // Prioritize pending approvals on initial load.
    this.filters.status = 'Pending';
    this.loadUsers();
  }

  loadUsers(): void {
    this.loading = true;
    this.errorMessage = '';

    this.userService.getAllUsers(this.filters).subscribe({
      next: (data) => {
        this.users = data;
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.errorMessage = err?.error?.message || 'Failed to load users.';
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }

  openUser(userId: number): void {
    this.userService.getUserById(userId).subscribe({
      next: (data) => {
        this.selectedUser = data;
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.errorMessage = err?.error?.message || 'Failed to load user details.';
        this.cdr.markForCheck();
      }
    });
  }

  viewCheckouts(userId: number): void {
    const user = this.users.find(u => u.userID === userId);
    if (!user || user.accountStatus !== 'Active') {
      return;
    }
    this.expandedUserId = userId;
    this.openDropdownId = null;
    this.openUser(userId);
  }

  private actionMessageTimeout: any;

  private showActionMessage(msg: string, type: 'success' | 'error' = 'success'): void {
    if (this.actionMessageTimeout) {
      clearTimeout(this.actionMessageTimeout);
    }
    this.actionMessageType = type;
    this.actionMessage = msg;
    this.cdr.markForCheck();
    this.actionMessageTimeout = setTimeout(() => {
      this.actionMessage = '';
      this.cdr.markForCheck();
    }, 4000);
  }

  private openConfirmation(title: string, message: string, action: () => void, cancelLabel = 'Cancel'): void {
    this.confirmationTitle = title;
    this.confirmationMessage = message;
    this.confirmationAction = action;
    this.confirmationCancelLabel = cancelLabel;
    this.confirmationModalOpen = true;
    this.cdr.markForCheck();
  }

  confirmAction(): void {
    const action = this.confirmationAction;
    this.confirmationModalOpen = false;
    this.confirmationAction = null;
    this.confirmationTitle = '';
    this.confirmationMessage = '';
    this.confirmationCancelLabel = 'Cancel';
    this.cdr.markForCheck();

    if (action) {
      action();
    }
  }

  cancelConfirmation(): void {
    this.confirmationModalOpen = false;
    this.confirmationAction = null;
    this.confirmationTitle = '';
    this.confirmationMessage = '';
    this.confirmationCancelLabel = 'Cancel';
    this.cdr.markForCheck();
  }

  approveUser(user: UserSummary): void {
    this.openConfirmation(
      'Approve User',
      `Approve ${user.fullName} (${user.email})? Click OK to complete the approval.`,
      () => this.executeApproveUser(user)
    );
  }

  private executeApproveUser(user: UserSummary): void {
    this.busyUserId = user.userID;
    this.userService.approveUser(user.userID).subscribe({
      next: (res) => {
        this.busyUserId = null;
        this.showActionMessage(`Approved ${user.fullName}.`);
        this.loadUsers();
      },
      error: (err) => {
        this.busyUserId = null;
        this.showActionMessage(err?.error?.message || 'Failed to approve user.', 'error');
        this.cdr.markForCheck();
      }
    });
  }

  openRejectModal(user: UserSummary): void {
    this.rejectTargetUser = user;
    this.rejectReason = '';
    this.rejectModalOpen = true;
    this.cdr.markForCheck();
  }

  cancelReject(): void {
    this.rejectModalOpen = false;
    this.rejectTargetUser = null;
    this.rejectReason = '';
    this.cdr.markForCheck();
  }

  confirmReject(): void {
    if (!this.rejectTargetUser || !this.rejectReason.trim()) return;
    const targetName = this.rejectTargetUser.fullName;
    this.busyUserId = this.rejectTargetUser.userID;
    this.userService.rejectUser(this.rejectTargetUser.userID, this.rejectReason.trim()).subscribe({
      next: (res) => {
        this.busyUserId = null;
        this.rejectModalOpen = false;
        this.rejectTargetUser = null;
        this.rejectReason = '';
        this.showActionMessage(`Rejected ${targetName}.`);
        this.loadUsers();
      },
      error: (err) => {
        this.busyUserId = null;
        this.showActionMessage(err?.error?.message || 'Failed to reject user.', 'error');
        this.cdr.markForCheck();
      }
    });
  }

  rejectUser(user: UserSummary): void {
    this.openRejectModal(user);
  }

  toggleActivation(user: UserSummary): void {
    const activate = user.accountStatus !== 'Active';
    if (!activate) {
      this.busyUserId = user.userID;
      this.cdr.markForCheck();
      this.userService.getUserById(user.userID).subscribe({
        next: (details) => {
          this.busyUserId = null;
          this.cdr.markForCheck();
          if (details.currentCheckouts && details.currentCheckouts.length > 0) {
            this.openConfirmation(
              'Deactivation Blocked',
              `User still has outstanding checkouts. You cannot deactivate a user who is currently holding company assets.`,
              () => {},
              ''
            );
          } else {
            this.openConfirmation(
              'Deactivate User',
              `Deactivate ${user.fullName} (${user.email})? Click OK to proceed.`,
              () => this.executeToggleActivation(user)
            );
          }
        },
        error: (err) => {
          this.busyUserId = null;
          this.cdr.markForCheck();
          this.showActionMessage(err?.error?.message || 'Failed to check user checkouts.', 'error');
        }
      });
    } else {
      this.openConfirmation(
        'Activate User',
        `Activate ${user.fullName} (${user.email})? Click OK to proceed.`,
        () => this.executeToggleActivation(user)
      );
    }
  }

  private executeToggleActivation(user: UserSummary): void {
    this.busyUserId = user.userID;
    const activate = user.accountStatus !== 'Active';
    this.userService.updateUserStatus(user.userID, activate).subscribe({
      next: (res) => {
        this.busyUserId = null;
        this.showActionMessage(`${activate ? 'Activated' : 'Deactivated'} ${user.fullName}.`);
        this.loadUsers();
      },
      error: (err) => {
        this.busyUserId = null;
        this.showActionMessage(err?.error?.message || 'Failed to update account status.', 'error');
        this.cdr.markForCheck();
      }
    });
  }

  changeRole(user: UserSummary, newRole: string): void {
    if (newRole === user.role) {
      return;
    }

    this.openConfirmation(
      'Change Role',
      `Change ${user.fullName}'s role from ${user.role} to ${newRole}? Click OK to apply the change.`,
      () => this.executeChangeRole(user, newRole)
    );
  }

  private executeChangeRole(user: UserSummary, newRole: string): void {
    this.busyUserId = user.userID;
    this.userService.updateUserRole(user.userID, newRole).subscribe({
      next: (res) => {
        this.busyUserId = null;
        this.showActionMessage(`Updated ${user.fullName}'s role to ${newRole}.`);
        this.loadUsers();
      },
      error: (err) => {
        this.busyUserId = null;
        this.showActionMessage(err?.error?.message || 'Failed to update user role.', 'error');
        this.cdr.markForCheck();
      }
    });
  }

  resetPassword(user: UserSummary): void {
    this.openConfirmation(
      'Reset Password',
      `Reset ${user.fullName}'s password? Click OK to generate a new temporary password.`,
      () => this.executeResetPassword(user)
    );
  }

  private executeResetPassword(user: UserSummary): void {
    this.busyUserId = user.userID;
    this.userService.resetUserPassword(user.userID).subscribe({
      next: (res) => {
        this.busyUserId = null;
        this.showActionMessage(`Reset password for ${user.fullName}. Temporary password: ${res.temporaryPassword}`);
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.busyUserId = null;
        this.showActionMessage(err?.error?.message || 'Failed to reset password.', 'error');
        this.cdr.markForCheck();
      }
    });
  }
}
