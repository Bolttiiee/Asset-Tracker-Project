import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

@Component({
  selector: 'app-confirmation-dialog',
  standalone: true,
  template: `
    @if (open()) {
      <div class="confirmation-backdrop" role="presentation">
        <div class="confirmation-card" role="dialog" aria-modal="true" aria-labelledby="confirmation-dialog-title" aria-describedby="confirmation-dialog-message">
          <h3 id="confirmation-dialog-title" class="confirmation-title">{{ title() }}</h3>
          <p id="confirmation-dialog-message" class="confirmation-message">{{ message() }}</p>
          <div class="confirmation-actions">
            @if (cancelLabel()) {
              <button type="button" class="confirmation-cancel" (click)="cancelled.emit()">{{ cancelLabel() }}</button>
            }
            <button type="button" class="confirmation-ok" (click)="confirmed.emit()">{{ confirmLabel() }}</button>
          </div>
        </div>
      </div>
    }
  `,
  styles: [`
    .confirmation-backdrop {
      position: fixed;
      inset: 0;
      background: rgba(0, 0, 0, 0.45);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 1100;
      padding: 1rem;
    }

    .confirmation-card {
      width: 100%;
      max-width: 420px;
      background: #ffffff;
      border-radius: 8px;
      padding: 1.5rem;
      box-shadow: 0 10px 40px rgba(0, 0, 0, 0.2);
    }

    .confirmation-title {
      margin: 0 0 0.5rem 0;
      color: #2d3748;
      font-size: 1.1rem;
    }

    .confirmation-message {
      margin: 0 0 1rem 0;
      color: #718096;
      font-size: 0.95rem;
      line-height: 1.5;
    }

    .confirmation-actions {
      display: flex;
      justify-content: flex-end;
      gap: 0.5rem;
    }

    .confirmation-cancel,
    .confirmation-ok {
      border-radius: 4px;
      padding: 0.45rem 0.9rem;
      cursor: pointer;
      font-weight: 600;
      border: none;
    }

    .confirmation-cancel {
      border: 1px solid #cbd5e0;
      background: #ffffff;
      color: #4a5568;
    }

    .confirmation-ok {
      background: #2b6cb0;
      color: #ffffff;
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ConfirmationDialogComponent {
  open = input(false);
  title = input('Confirm Action');
  message = input('Are you sure you want to continue?');
  confirmLabel = input('OK');
  cancelLabel = input('Cancel');

  confirmed = output<void>();
  cancelled = output<void>();
}