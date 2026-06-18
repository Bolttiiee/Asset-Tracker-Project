import { CommonModule } from '@angular/common';
import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { AssetService } from '../../services/asset';
import { Asset } from '../../models/asset.model';
import { DrawerComponent } from '../drawer/drawer';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-asset-modal',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, DrawerComponent],
  templateUrl: './asset-modal.html',
  styleUrls: ['./asset-modal.css']
})
export class AssetModalComponent implements OnInit {
  @Output() assetCreated = new EventEmitter<void>();

  isOpen = false;
  isSubmitting = false;
  mode: 'create' | 'edit' = 'create';
  editingAsset: Asset | null = null;
  serialValidationError = '';
  assetForm!: FormGroup;

  constructor(private fb: FormBuilder, private assetService: AssetService) {}

  ngOnInit(): void {
    this.initForm();
  }

  initForm(): void {
    this.assetForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(50)]],
      category: ['', [Validators.required, Validators.maxLength(100)]],
      serialNumber: ['', [Validators.required, Validators.pattern('^[A-Z0-9]{8,20}$')]],
      condition: ['Good', [Validators.required]],
      value: ['', [Validators.required, Validators.min(0.01)]],
      status: ['Available', [Validators.required]],
      notes: ['', [Validators.maxLength(500)]]
    });
  }

  get drawerTitle(): string {
    return this.mode === 'create' ? 'Provision New Corporate Asset' : 'Edit Corporate Asset';
  }

  // Backward-compatible alias used by inventory component.
  openModal(): void {
    this.openForCreate();
  }

  openForCreate(): void {
    this.mode = 'create';
    this.editingAsset = null;
    this.serialValidationError = '';
    this.assetForm.reset({ status: 'Available', condition: 'Good', notes: '' });
    this.isOpen = true;
  }

  openForEdit(asset: Asset): void {
    this.mode = 'edit';
    this.editingAsset = asset;
    this.serialValidationError = '';
    this.assetForm.reset({
      name: asset.name,
      category: asset.category,
      serialNumber: asset.serialNumber,
      condition: asset.condition || 'Good',
      value: asset.value,
      status: asset.status,
      notes: asset.notes || ''
    });
    this.isOpen = true;
  }

  closeModal(): void {
    if (this.isSubmitting) {
      return;
    }
    this.isOpen = false;
    this.isSubmitting = false;
    this.mode = 'create';
    this.editingAsset = null;
    this.serialValidationError = '';
    this.assetForm.reset({ status: 'Available', condition: 'Good', notes: '' });
  }

  submitAsset(): void {
    // Fail immediately if someone bypassed disabled UI controls
    if (this.assetForm.invalid) {
      this.assetForm.markAllAsTouched();
      return;
    }

    this.serialValidationError = '';
    this.isSubmitting = true;

    const formValue = this.assetForm.getRawValue();
    const normalizedSerial = String(formValue.serialNumber || '').trim().toUpperCase();
    const editingAssetId = this.editingAsset?.assetID ?? null;

    // Async uniqueness check against all assets, including decommissioned entries.
    this.assetService.getAssets().subscribe({
      next: (assets) => {
        const serialInUse = assets.some((asset) =>
          String(asset.serialNumber || '').trim().toUpperCase() === normalizedSerial && asset.assetID !== editingAssetId
        );

        if (serialInUse) {
          this.isSubmitting = false;
          this.serialValidationError = 'Serial number already exists (including decommissioned assets).';
          return;
        }

        const payload: Partial<Asset> = {
          ...(this.editingAsset ?? {}),
          ...formValue,
          serialNumber: normalizedSerial
        };

        const request$ = this.mode === 'edit' && this.editingAsset
          ? this.assetService.updateAsset(this.editingAsset.assetID, payload as Asset)
          : this.assetService.createAsset(payload);

        request$.pipe(
          finalize(() => this.isSubmitting = false)
        ).subscribe({
          next: () => {
            this.assetCreated.emit();
            this.closeModal();
          },
          error: (err) => {
            alert(err?.error?.message || (this.mode === 'edit' ? 'Failed to update asset.' : 'Failed to provision asset.'));
          }
        });
      },
      error: () => {
        this.isSubmitting = false;
        alert('Failed to verify serial number uniqueness.');
      }
    });
  }
}
