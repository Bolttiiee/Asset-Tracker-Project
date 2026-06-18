import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit } from '@angular/core';
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

  confirmationModalOpen = false;
  confirmationTitle = '';
  confirmationMessage = '';
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

  private showActionMessage(msg: string, type: 'success' | 'error' = 'success'): void {
    this.actionMessageType = type;
    this.actionMessage = msg;
    this.cdr.markForCheck();
    setTimeout(() => {
      this.actionMessage = '';
      this.cdr.markForCheck();
    }, 4000);
  }

  private openConfirmation(title: string, message: string, action: () => void): void {
    this.confirmationTitle = title;
    this.confirmationMessage = message;
    this.confirmationAction = action;
    this.confirmationModalOpen = true;
    this.cdr.markForCheck();
  }

  confirmAction(): void {
    const action = this.confirmationAction;
    this.confirmationModalOpen = false;
    this.confirmationAction = null;
    this.confirmationTitle = '';
    this.confirmationMessage = '';
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
    this.openConfirmation(
      activate ? 'Activate User' : 'Deactivate User',
      `${activate ? 'Activate' : 'Deactivate'} ${user.fullName} (${user.email})? Click OK to proceed.`,
      () => this.executeToggleActivation(user)
    );
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
