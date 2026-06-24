import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CheckoutHistoryRow, ReportDropdownAsset, ReportDropdownUser, ReportFilters, ReportService } from '../../services/report';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './reports.html',
  styleUrls: ['./reports.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ReportsComponent implements OnInit {
  rows: CheckoutHistoryRow[] = [];
  categories: string[] = [];
  assets: ReportDropdownAsset[] = [];
  users: ReportDropdownUser[] = [];

  loading = false;
  exportingExcel = false;
  exportingPdf = false;
  errorMessage = '';

  filters: ReportFilters = {
    fromDate: '',
    toDate: '',
    category: 'All'
  };

  constructor(private reportService: ReportService, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.loadDropdowns();
    this.runSearch();
  }

  loadDropdowns(): void {
    this.reportService.getCategories().subscribe({
      next: (data) => {
        this.categories = data;
        this.cdr.markForCheck();
      }
    });

    this.reportService.getAssets().subscribe({
      next: (data) => {
        this.assets = data;
        this.cdr.markForCheck();
      }
    });

    this.reportService.getUsers().subscribe({
      next: (data) => {
        this.users = data;
        this.cdr.markForCheck();
      }
    });
  }

  get dateRangeError(): string {
    const { fromDate, toDate } = this.filters;
    if (fromDate && toDate && fromDate > toDate) {
      return '"From" date must be on or before the "To" date.';
    }
    return '';
  }

  runSearch(): void {
    this.loading = true;
    this.errorMessage = '';

    this.reportService.getCheckoutHistory(this.filters).subscribe({
      next: (data) => {
        this.rows = data;
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.errorMessage = err?.error?.message || 'Failed to load report data.';
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }

  viewAll(): void {
    this.filters = {
      fromDate: '',
      toDate: '',
      category: 'All',
      assetId: undefined,
      userId: undefined
    };
    this.runSearch();
  }

  downloadExcel(): void {
    this.exportingExcel = true;
    this.reportService.downloadExcel(this.filters).pipe(
      finalize(() => {
        this.exportingExcel = false;
        this.cdr.markForCheck();
      })
    ).subscribe((blob) => {
      this.downloadBlob(blob, 'checkout-history.xlsx');
    });
  }

  downloadPdf(): void {
    this.exportingPdf = true;
    this.reportService.downloadPdf(this.filters).pipe(
      finalize(() => {
        this.exportingPdf = false;
        this.cdr.markForCheck();
      })
    ).subscribe((blob) => {
      this.downloadBlob(blob, 'checkout-history.pdf');
    });
  }

  private downloadBlob(blob: Blob, filename: string): void {
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    link.click();
    window.URL.revokeObjectURL(url);
  }
}
