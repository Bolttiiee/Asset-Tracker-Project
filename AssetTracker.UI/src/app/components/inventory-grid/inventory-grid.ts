import { ChangeDetectorRef, Component, HostListener, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AssetService } from '../../services/asset';
import { UserService, UserSummary } from '../../services/user';
import { AdminService } from '../../services/admin';
import { Asset } from '../../models/asset.model';
import { AssetModalComponent } from '../asset-modal/asset-modal';
import { ConfirmationDialogComponent } from '../confirmation-dialog/confirmation-dialog';
import { DrawerComponent } from '../drawer/drawer';
import { StatusChipComponent } from '../status-chip/status-chip';

@Component({
  selector: 'app-inventory-grid',
  standalone: true,
  imports: [CommonModule, FormsModule, AssetModalComponent, ConfirmationDialogComponent, DrawerComponent, StatusChipComponent],
  templateUrl: './inventory-grid.html',
  styleUrls: ['./inventory-grid.css']
})
export class InventoryGridComponent implements OnInit {
  assets: Asset[] = [];
  loading = false;
  expandedAssetId: number | null = null;
  userRole: string = '';
  errorMessage: string = '';
  apiConnectionMessage: string = '';
  toastMessage: string = '';
  toastType: 'success' | 'error' | 'info' = 'info';

  toggleExpand(id: number, event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (target.closest('.btn-action-trigger') || target.closest('.action-dropdown-menu') || target.closest('button')) {
      return;
    }
    if (this.expandedAssetId === id) {
      this.expandedAssetId = null;
    } else {
      this.expandedAssetId = id;
    }
  }
  checkoutDrawerOpen = false;
  isHeaderAssign = false;
  checkoutAssetId: number | null = null;
  checkoutAssetName: string = '';

  get availableAssets(): Asset[] {
    return this.assets.filter(a => a.status === 'Available');
  }
  checkoutFullName: string = '';
  checkoutEmail: string = '';
  checkoutExpectedReturnDate: string = '';
  employeeOptions: UserSummary[] = [];
  selectedEmployeeId: number | null = null;
  employeeSearch: string = '';
  today = new Intl.DateTimeFormat('sv-SE', { timeZone: 'Africa/Johannesburg', year: 'numeric', month: '2-digit', day: '2-digit' }).format(new Date());
  metrics: any = null;
  searchTerm: string = '';
  statusFilter: string = '';
  categoryFilter: string = '';
  minValueFilter: number | null = null;
  busyAssetId: number | null = null;
  confirmationModalOpen = false;
  confirmationTitle = '';
  confirmationMessage = '';
  private confirmationAction: (() => void) | null = null;
  
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

  get filteredAssets(): any[] {
    return this.assets.filter(asset => {
      // Standard users never see decommissioned assets
      if (!this.isAdminOrManager() && asset.status === 'Decommissioned') return false;

      const matchesSearch = !this.searchTerm ? true :
        asset.name.toLowerCase().includes(this.searchTerm.toLowerCase()) ||
        asset.serialNumber.toLowerCase().includes(this.searchTerm.toLowerCase()) ||
        (asset.category || '').toLowerCase().includes(this.searchTerm.toLowerCase());

      const matchesStatus = !this.statusFilter ? true :
        asset.status === this.statusFilter;

      const matchesCategory = !this.categoryFilter ? true :
        asset.category === this.categoryFilter;

      const matchesValue = this.minValueFilter === null ? true :
        asset.value >= this.minValueFilter;

      return matchesSearch && matchesStatus && matchesCategory && matchesValue;
    });
  }

  get availableCategories(): string[] {
    const cats = this.assets.map(a => a.category).filter(Boolean);
    return [...new Set(cats)].sort();
  }

  get filteredEmployees(): UserSummary[] {
    const term = this.employeeSearch.trim().toLowerCase();
    if (!term) {
      return this.employeeOptions;
    }
    return this.employeeOptions.filter((user) =>
      user.fullName.toLowerCase().includes(term) ||
      user.email.toLowerCase().includes(term) ||
      (user.department || '').toLowerCase().includes(term)
    );
  }

  // Structural link to the child modal for imperative open calls
  @ViewChild('provisionModal') provisionModal!: AssetModalComponent;

  constructor(private assetService: AssetService, private userService: UserService, private adminService: AdminService, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    // Read the role we stored during login, with a fallback to the saved user profile.
    const storedUserProfile = localStorage.getItem('user_profile');
    const parsedUserProfile = storedUserProfile ? JSON.parse(storedUserProfile) : null;
    this.userRole = localStorage.getItem('userRole') || parsedUserProfile?.role || parsedUserProfile?.userRole || 'Standard User';

    // Per §2.3: standard users should only see available assets by default in the checkout list.
    // Admins/managers need to see all statuses (including decommissioned) so no default filter is applied.
    if (!this.isAdminOrManager()) {
      this.statusFilter = 'Available';
    } else {
      // Admins/managers assign assets to any employee, so load the selectable list.
      this.loadEmployees();
    }

    this.loadInventory();
  }

  loadEmployees(): void {
    this.userService.getAllUsers().subscribe({
      next: (users) => {
        // Only active accounts can receive assets; present them alphabetically by name.
        this.employeeOptions = (users || [])
          .filter((user) => user.accountStatus === 'Active')
          .sort((a, b) => a.fullName.localeCompare(b.fullName));
        this.cdr.markForCheck();
      },
      error: () => {
        // Non-fatal: the manual name/email entry still works if the list can't load.
        this.employeeOptions = [];
        this.cdr.markForCheck();
      }
    });
  }

  onEmployeeSelected(userID: number | null): void {
    const employee = this.employeeOptions.find((user) => user.userID === userID);
    if (employee) {
      this.checkoutFullName = employee.fullName;
      this.checkoutEmail = employee.email;
      this.cdr.markForCheck();
    }
  }

  loadInventory(): void {
    this.apiConnectionMessage = '';
    this.loading = true;

    this.assetService.getAssets().subscribe({
      next: (data) => {
        this.assets = data;
        this.apiConnectionMessage = '';
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.apiConnectionMessage = this.resolveApiConnectionMessage(err, 'inventory');
        this.errorMessage = err?.status === 401
          ? 'Session expired — please log in again.'
          : err?.status === 403
          ? 'Access denied — insufficient role permissions.'
          : `Failed to load corporate inventory (HTTP ${err?.status}). Check the API server.`;
        console.error(err);
        this.loading = false;
        this.cdr.markForCheck();
      }
    });

    // Fetch your fresh metric aggregates if the user is an authorized admin/manager
    if (this.isAdminOrManager()) {
      this.assetService.getDashboardMetrics().subscribe({
        next: (data) => {
          this.metrics = data;
          this.cdr.markForCheck();
        },
        error: (err) => {
          this.apiConnectionMessage = this.resolveApiConnectionMessage(err, 'dashboard metrics');
          console.error('Dashboard metrics access restricted or failed.', err);
          this.cdr.markForCheck();
        }
      });
    }
  }

  // Quick check for HTML view restrictions
  isAdminOrManager(): boolean {
    return this.userRole === 'Admin' || this.userRole === 'Asset Manager';
  }

  isAdmin(): boolean {
    return this.userRole === 'Admin';
  }

  runningReminders = false;

  runOverdueReminders(): void {
    this.runningReminders = true;
    this.cdr.markForCheck();
    this.adminService.runOverdueReminders().subscribe({
      next: (result) => {
        this.runningReminders = false;
        const sent = result?.remindersSent ?? 0;
        const overdue = result?.overdueCount ?? 0;
        const digest = result?.digestSent ? ' Admin digest sent.' : '';
        this.showToast(
          `Overdue sweep complete: ${overdue} overdue, ${sent} reminder(s) sent.${digest}`,
          'success'
        );
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.runningReminders = false;
        this.showToast(err?.error?.message || 'Failed to run overdue reminders.', 'error');
        this.cdr.markForCheck();
      }
    });
  }

  // Trigger modal open from the grid action button
  openProvisioningModal(): void {
    this.provisionModal.openForCreate();
  }

  openEditAssetDrawer(asset: Asset): void {
    this.provisionModal.openForEdit(asset);
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

  decommissionAsset(asset: Asset): void {
    this.openConfirmation(
      'Decommission Asset',
      `Decommission "${asset.name}"? Click OK to proceed.`,
      () => this.executeDecommissionAsset(asset)
    );
  }

  private executeDecommissionAsset(asset: Asset): void {
    this.busyAssetId = asset.assetID;
    const updatedAsset: Asset = {
      ...asset,
      status: 'Decommissioned',
      assignedTo: ''
    };

    this.assets = this.assets.map((currentAsset) =>
      currentAsset.assetID === asset.assetID ? updatedAsset : currentAsset
    );
    this.refreshLocalMetrics();
    this.cdr.markForCheck();

    // Decommission is a soft delete on the backend (DELETE endpoint stamps DecommissionDate).
    // The PUT/update endpoint explicitly rejects direct status changes.
    this.assetService.deleteAsset(asset.assetID).subscribe({
      next: () => {
        this.busyAssetId = null;
        this.showToast('Asset decommissioned successfully.', 'success');
        this.loadInventory();
      },
      error: (err) => {
        this.busyAssetId = null;
        // Revert local state to the original asset
        this.assets = this.assets.map((currentAsset) =>
          currentAsset.assetID === asset.assetID ? asset : currentAsset
        );
        this.refreshLocalMetrics();
        this.cdr.markForCheck();
        this.showToast(err?.error?.message || 'Decommission failed. This asset may require backend policy handling.', 'error');
        this.loadInventory();
      }
    });
  }

  forceReturnAsset(asset: Asset): void {
    const assignedUser = asset.assignedTo ? ` (currently assigned to ${asset.assignedTo})` : '';
    this.openConfirmation(
      'Force Return Asset',
      `Force return "${asset.name}"${assignedUser}? This will mark it as Available and remove it from that user's My Assets. Click OK to proceed.`,
      () => this.executeForceReturnAsset(asset)
    );
  }

  private executeForceReturnAsset(asset: Asset): void {
    this.busyAssetId = asset.assetID;
    this.assetService.adminOverrideReturn(asset.assetID).subscribe({
      next: (res) => {
        this.busyAssetId = null;
        this.assets = this.assets.map((currentAsset) =>
          currentAsset.assetID === asset.assetID
            ? { ...currentAsset, status: 'Available', assignedTo: '' }
            : currentAsset
        );
        this.refreshLocalMetrics();
        this.cdr.markForCheck();
        this.showToast(res?.message || 'Force return successful.', 'success');
        this.loadInventory();
      },
      error: (err) => {
        this.busyAssetId = null;
        this.showToast(err?.error?.message || 'Force return failed.', 'error');
      }
    });
  }

  cancelCheckout(asset: Asset): void {
    const assignedUser = asset.assignedTo ? ` (currently assigned to ${asset.assignedTo})` : '';
    this.openConfirmation(
      'Cancel Checkout',
      `Cancel the active checkout for "${asset.name}"${assignedUser}? This voids the checkout and returns the asset to Available. Click OK to proceed.`,
      () => this.executeCancelCheckout(asset)
    );
  }

  private executeCancelCheckout(asset: Asset): void {
    this.busyAssetId = asset.assetID;
    this.assetService.cancelCheckout(asset.assetID).subscribe({
      next: (res) => {
        this.busyAssetId = null;
        this.assets = this.assets.map((currentAsset) =>
          currentAsset.assetID === asset.assetID
            ? { ...currentAsset, status: 'Available', assignedTo: '' }
            : currentAsset
        );
        this.refreshLocalMetrics();
        this.cdr.markForCheck();
        this.showToast(res?.message || 'Checkout cancelled. Asset returned to Available.', 'success');
        this.loadInventory();
      },
      error: (err) => {
        this.busyAssetId = null;
        this.showToast(err?.error?.message || 'Cancel checkout failed.', 'error');
      }
    });
  }

  restoreAsset(asset: Asset): void {
    this.openConfirmation(
      'Restore Asset',
      `Restore "${asset.name}" to Available status? Click OK to proceed.`,
      () => this.executeRestoreAsset(asset)
    );
  }

  private executeRestoreAsset(asset: Asset): void {
    this.busyAssetId = asset.assetID;
    this.assetService.restoreAsset(asset.assetID).subscribe({
      next: (res) => {
        this.busyAssetId = null;
        this.assets = this.assets.map((currentAsset) =>
          currentAsset.assetID === asset.assetID
            ? { ...currentAsset, status: 'Available', assignedTo: '' }
            : currentAsset
        );
        this.refreshLocalMetrics();
        this.cdr.markForCheck();
        this.showToast(res?.message || 'Asset restored successfully.', 'success');
        this.loadInventory();
      },
      error: (err) => {
        this.busyAssetId = null;
        this.showToast(err?.error?.message || 'Restore failed.', 'error');
      }
    });
  }

  toggleMaintenance(asset: Asset, setMaintenance: boolean): void {
    this.openConfirmation(
      setMaintenance ? 'Set Under Maintenance' : 'Clear Maintenance',
      setMaintenance
        ? `Set "${asset.name}" to Under Maintenance? It will not be available for checkout until cleared. Click OK to proceed.`
        : `Clear maintenance on "${asset.name}" and mark it as Available? Click OK to proceed.`,
      () => this.executeToggleMaintenance(asset, setMaintenance)
    );
  }

  private executeToggleMaintenance(asset: Asset, setMaintenance: boolean): void {
    this.busyAssetId = asset.assetID;
    this.assetService.setMaintenanceStatus(asset.assetID, setMaintenance).subscribe({
      next: (res) => {
        this.busyAssetId = null;
        this.assets = this.assets.map((currentAsset) =>
          currentAsset.assetID === asset.assetID
            ? { ...currentAsset, status: setMaintenance ? 'Under Maintenance' : 'Available' }
            : currentAsset
        );
        this.refreshLocalMetrics();
        this.cdr.markForCheck();
        this.showToast(
          res?.message || (setMaintenance ? 'Asset set to maintenance.' : 'Asset removed from maintenance.'),
          'success'
        );
        this.loadInventory();
      },
      error: (err) => {
        this.busyAssetId = null;
        this.showToast(err?.error?.message || 'Maintenance status update failed.', 'error');
      }
    });
  }

  openCheckoutDrawer(asset: Asset): void {
    if (asset.status !== 'Available') {
      this.showToast('Checkout rejected: only available assets can be assigned.', 'error');
      return;
    }

    this.isHeaderAssign = false;
    this.checkoutAssetId = asset.assetID;
    this.checkoutAssetName = asset.name;

    // Pre-fill with the logged-in user's own details for standard users
    if (!this.isAdminOrManager()) {
      const profile = localStorage.getItem('user_profile');
      const parsed = profile ? JSON.parse(profile) : null;
      this.checkoutFullName = parsed?.fullName || parsed?.FullName || '';
      this.checkoutEmail = parsed?.email || parsed?.Email || '';
    } else {
      this.checkoutFullName = '';
      this.checkoutEmail = '';
    }

    this.checkoutExpectedReturnDate = '';
    this.checkoutDrawerOpen = true;
  }

  closeCheckoutDrawer(isSuccess: boolean = false): void {
    if (this.checkoutDrawerOpen && !isSuccess) {
      this.showToast('Checkout cancelled. No asset was assigned.', 'info');
    }
    this.checkoutDrawerOpen = false;
    this.isHeaderAssign = false;
    this.clearCheckoutForm();
  }

  openHeaderCheckoutDrawer(): void {
    const available = this.availableAssets;
    if (available.length === 0) {
      this.showToast('Assign rejected: no available assets in inventory.', 'error');
      return;
    }

    this.isHeaderAssign = true;
    this.checkoutAssetId = null;
    this.checkoutAssetName = '';
    this.clearEmployeeFields();
    this.checkoutDrawerOpen = true;
  }

  onHeaderAssetSelected(assetId: number | null): void {
    const asset = this.assets.find(a => a.assetID === assetId);
    if (asset) {
      this.checkoutAssetName = asset.name;
    } else {
      this.checkoutAssetName = '';
    }
  }

  clearCheckoutFormWithNotice(): void {
    this.clearEmployeeFields();
    this.showToast('Checkout form cleared.', 'info');
  }

  clearEmployeeFields(): void {
    this.checkoutFullName = '';
    this.checkoutEmail = '';
    this.selectedEmployeeId = null;
    this.employeeSearch = '';
    this.checkoutExpectedReturnDate = '';
  }

  clearCheckoutForm(): void {
    this.checkoutAssetId = null;
    this.checkoutAssetName = '';
    this.isHeaderAssign = false;
    this.clearEmployeeFields();
  }

  submitCheckout(): void {
    if (this.checkoutAssetId === null) {
      this.showToast('No asset selected for checkout.', 'error');
      return;
    }
    this.handleCheckout(this.checkoutAssetId);
  }

  handleCheckout(assetId: number): void {
    const selectedAsset = this.assets.find((asset) => asset.assetID === assetId);
    if (!selectedAsset) {
      this.showToast('No asset selected for checkout.', 'error');
      return;
    }

    if (selectedAsset.status === 'Decommissioned') {
      this.showToast('Checkout rejected: decommissioned assets cannot be assigned.', 'error');
      return;
    }

    if (selectedAsset.status !== 'Available') {
      this.showToast('Checkout rejected: only available assets can be assigned.', 'error');
      return;
    }

    const fullName = this.checkoutFullName.trim();
    const email = this.checkoutEmail.trim();
    const expectedReturnDate = this.checkoutExpectedReturnDate;

    if (!fullName || !email || !expectedReturnDate) {
      this.showToast('Checkout rejected: missing required full name, email, or expected return date.', 'error');
      return;
    }

    const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailPattern.test(email)) {
      this.showToast('Checkout rejected: enter a valid email address.', 'error');
      return;
    }

    const expectedDate = new Date(expectedReturnDate);
    expectedDate.setHours(0, 0, 0, 0);
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    if (Number.isNaN(expectedDate.getTime()) || expectedDate < today) {
      this.showToast('Checkout rejected: expected return date is missing or in the past.', 'error');
      return;
    }

    const duplicateCheckout = this.assets.some((asset) =>
      asset.assetID === assetId &&
      asset.status === 'Checked Out' &&
      (asset.assignedTo || '').toLowerCase().includes(email.toLowerCase())
    );
    if (duplicateCheckout) {
      this.showToast('Checkout rejected: this user already has the same asset checked out.', 'error');
      return;
    }

    const durationMs = expectedDate.getTime() - today.getTime();
    const durationDays = Math.max(1, Math.ceil(durationMs / (1000 * 60 * 60 * 24)));

    this.openConfirmation(
      'Assign Asset',
      `Confirm checkout for ${selectedAsset.name} to ${fullName}? Click OK to proceed.`,
      () => this.executeCheckout({ assetId, email, durationDays, fullName, expectedReturnDate })
    );
  }

  private executeCheckout(details: {
    assetId: number;
    email: string;
    durationDays: number;
    fullName: string;
    expectedReturnDate: string;
  }): void {
    this.busyAssetId = details.assetId;

    (this.assetService as any).assignAsset(details.assetId, details.email, details.durationDays, details.fullName, details.expectedReturnDate).subscribe({
      next: (res: any) => {
        this.busyAssetId = null;
        // Optimistically reflect assignment in the UI before the backend refresh completes.
        this.assets = this.assets.map((asset) =>
          asset.assetID === details.assetId
            ? { ...asset, status: 'Checked Out', assignedTo: `${details.fullName} (${details.email})` }
            : asset
        );
        this.refreshLocalMetrics();
        this.cdr.markForCheck();

        this.showToast('Checkout successful.', 'success');
        if (res?.message) {
          console.log(res.message);
        }
        this.closeCheckoutDrawer(true);
        this.loadInventory(); // Sync with canonical backend state
      },
      error: (err: any) => {
        this.busyAssetId = null;
        this.showToast(err.error?.message || 'Checkout rejected. Check required transaction data.', 'error');
      }
    });
  }

  handleReturn(assetId: number): void {
    this.openConfirmation(
      'Return Asset',
      'Confirm this hardware asset has been physically returned to inventory storage? Click OK to proceed.',
      () => this.executeReturn(assetId)
    );
  }

  private executeReturn(assetId: number): void {
    this.busyAssetId = assetId;
    this.assetService.returnAsset(assetId).subscribe({
      next: (res) => {
        this.busyAssetId = null;
        this.assets = this.assets.map((asset) =>
          asset.assetID === assetId
            ? { ...asset, status: 'Available', assignedTo: '' }
            : asset
        );
        this.refreshLocalMetrics();
        this.cdr.markForCheck();
        if (res?.message) console.log(res.message);
        this.showToast('Asset return successful. Status restored to Available.', 'success');
        this.loadInventory();
      },
      error: (err) => {
        this.busyAssetId = null;
        this.showToast(err.error?.message || 'Return processing failed.', 'error');
      }
    });
  }

  private toastTimeout: any;

  private showToast(message: string, type: 'success' | 'error' | 'info'): void {
    if (this.toastTimeout) {
      clearTimeout(this.toastTimeout);
    }
    this.toastMessage = message;
    this.toastType = type;
    this.cdr.markForCheck();
    this.toastTimeout = setTimeout(() => {
      this.toastMessage = '';
      this.cdr.markForCheck();
    }, 3000);
  }

  private resolveApiConnectionMessage(err: any, area: string): string {
    if (err?.status === 0) {
      return `Unable to reach backend API while loading ${area}. Ensure https://localhost:7175 is running.`;
    }
    if (err?.status === 401) {
      return `API responded with 401 for ${area}. Re-login may be required.`;
    }
    if (err?.status === 403) {
      return `API responded with 403 for ${area}. Your role may not have access.`;
    }
    return `API returned an error (${err?.status ?? 'unknown'}) while loading ${area}.`;
  }

  private refreshLocalMetrics(): void {
    if (!this.metrics || this.assets.length === 0) {
      return;
    }

    const totalAssets = this.assets.length;
    const totalValuation = this.assets.reduce((sum, asset) => sum + (asset.value || 0), 0);
    const averageAssetCost = totalAssets > 0 ? totalValuation / totalAssets : 0;

    const distribution = {
      available: this.assets.filter((asset) => asset.status === 'Available').length,
      checkedOut: this.assets.filter((asset) => asset.status === 'Checked Out').length,
      underMaintenance: this.assets.filter((asset) => asset.status === 'Under Maintenance').length,
      decommissioned: this.assets.filter((asset) => asset.status === 'Decommissioned').length
    };

    this.metrics = {
      totalAssets,
      totalValuation,
      averageAssetCost,
      distribution
    };
  }
}
