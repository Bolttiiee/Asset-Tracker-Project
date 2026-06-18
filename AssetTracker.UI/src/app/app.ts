import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { UserService } from './services/user';

@Component({
  selector: 'app-root',
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  protected readonly title = signal('AssetTracker.UI');

  isLoggedIn(): boolean {
    return !!(localStorage.getItem('token') || localStorage.getItem('asset_tracker_token'));
  }
  
  pendingApprovalsCount = 0;

  constructor(
    private userService: UserService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadPendingCount();
    setInterval(() => this.loadPendingCount(), 10000);
  }

  private loadPendingCount(): void {
    if (this.isAdmin()) {
      this.userService.getAllUsers({ status: 'Pending' }).subscribe({
        next: (users) => {
          this.pendingApprovalsCount = users.length;
        },
        error: () => {}
      });
    }
  }

  getUserRole(): string {
    const storedUserProfile = localStorage.getItem('user_profile');
    const parsedUserProfile = storedUserProfile ? JSON.parse(storedUserProfile) : null;
    return localStorage.getItem('userRole') || parsedUserProfile?.role || parsedUserProfile?.userRole || 'Standard User';
  }

  isAdminOrManager(): boolean {
    const role = this.getUserRole();
    return role === 'Admin' || role === 'Asset Manager';
  }

  isAdmin(): boolean {
    return this.getUserRole() === 'Admin';
  }

  isAuthRoute(): boolean {
    const path = this.router.url.split('?')[0];
    return path === '/login' || path === '/register';
  }

  logout(): void {
    localStorage.removeItem('token');
    localStorage.removeItem('asset_tracker_token');
    localStorage.removeItem('userRole');
    localStorage.removeItem('user_profile');
  }
}
