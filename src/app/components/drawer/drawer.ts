import { Component, Input, Output, EventEmitter, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-drawer',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './drawer.html',
  styleUrls: ['./drawer.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DrawerComponent {
  @Input() isOpen = false;
  @Input() title = '';
  @Input() width = '400px'; // Adjustable width
  
  @Output() closeDrawer = new EventEmitter<void>();
  @Output() confirmAction = new EventEmitter<void>();

  onClose(): void {
    this.closeDrawer.emit();
  }

  onConfirm(): void {
    this.confirmAction.emit();
  }

  // Prevent close on backdrop click in checkout context (can be overridden)
  onBackdropClick(): void {
    this.onClose();
  }
}
