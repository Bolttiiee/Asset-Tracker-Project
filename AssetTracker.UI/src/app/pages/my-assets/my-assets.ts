import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CheckoutService, MyCheckoutAsset } from '../../services/checkout';
import { StatusChipComponent } from '../../components/status-chip/status-chip';
import { ConfirmationDialogComponent } from '../../components/confirmation-dialog/confirmation-dialog';

@Component({
  selector: 'app-my-assets',
  standalone: true,
  imports: [CommonModule, StatusChipComponent, ConfirmationDialogComponent],
  templateUrl: './my-assets.html',
  styleUrls: ['./my-assets.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MyAssetsComponent implements OnInit {
  assets: MyCheckoutAsset[] = [];
  loading = false;
  errorMessage = '';
  successMessage = '';
  returningAssetId: number | null = null;

  confirmationModalOpen = false;
  confirmationTitle = '';
  confirmationMessage = '';
  private pendingReturnAsset: MyCheckoutAsset | null = null;

  constructor(private checkoutService: CheckoutService, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.loadMyAssets();
  }

  loadMyAssets(): void {
    this.loading = true;
    this.errorMessage = '';

    this.checkoutService.getMyAssets().subscribe({
      next: (data) => {
        this.assets = data;
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.errorMessage = err?.error?.message || 'Unable to load your checked-out assets.';
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }

  getDaysOverdue(expectedReturnDate: string): number {
    const expected = new Date(expectedReturnDate);
    const today = new Date();
    const diffTime = today.getTime() - expected.getTime();
    const diffDays = Math.floor(diffTime / (1000 * 60 * 60 * 24));
    return diffDays > 0 ? diffDays : 0;
  }

  returnAsset(asset: MyCheckoutAsset): void {
    this.confirmationTitle = 'Return Asset';
    this.confirmationMessage = `Return "${asset.assetName}"? This will mark the asset as Available.`;
    this.pendingReturnAsset = asset;
    this.confirmationModalOpen = true;
    this.cdr.markForCheck();
  }

  private successTimeout: any;
  private errorTimeout: any;

  confirmReturn(): void {
    const asset = this.pendingReturnAsset;
    this.confirmationModalOpen = false;
    this.pendingReturnAsset = null;
    this.confirmationTitle = '';
    this.confirmationMessage = '';
    this.cdr.markForCheck();

    if (!asset) return;

    this.returningAssetId = asset.assetID;

    // Clear previous timeouts and messages
    if (this.successTimeout) clearTimeout(this.successTimeout);
    if (this.errorTimeout) clearTimeout(this.errorTimeout);
    this.successMessage = '';
    this.errorMessage = '';

    this.checkoutService.returnAsset(asset.assetID).subscribe({
      next: () => {
        this.returningAssetId = null;
        this.successMessage = `${asset.assetName} returned successfully. Status restored to Available.`;
        this.cdr.markForCheck();
        this.successTimeout = setTimeout(() => {
          this.successMessage = '';
          this.cdr.markForCheck();
        }, 4000);
        this.loadMyAssets();
      },
      error: (err: any) => {
        this.returningAssetId = null;
        this.errorMessage = err?.error?.message || 'Return failed.';
        this.cdr.markForCheck();
        this.errorTimeout = setTimeout(() => {
          this.errorMessage = '';
          this.cdr.markForCheck();
        }, 4000);
      }
    });
  }

  cancelReturn(): void {
    this.confirmationModalOpen = false;
    this.pendingReturnAsset = null;
    this.confirmationTitle = '';
    this.confirmationMessage = '';
    this.cdr.markForCheck();
  }
}
