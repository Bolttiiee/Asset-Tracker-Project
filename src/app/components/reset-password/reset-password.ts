import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../services/auth';

@Component({
  selector: 'app-reset-password',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './reset-password.html',
  styleUrl: './reset-password.css',
})
export class ResetPasswordComponent implements OnInit {
  resetForm!: FormGroup;
  token = '';
  forced = false;
  successMessage = '';
  errorMessage = '';

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.token = this.route.snapshot.queryParams['token'] || '';
    this.forced = this.route.snapshot.queryParams['forced'] === 'true';

    if (!this.token && !this.forced) {
      this.errorMessage = 'Security violation: Missing verification token. Request a new link.';
    }

    this.resetForm = this.fb.group({
      password: ['', [Validators.required, Validators.minLength(6)]],
    });
  }

  submitNewPassword(): void {
    if (this.resetForm.invalid) {
      return;
    }

    if (this.forced) {
      this.authService.changePassword(this.resetForm.value.password).subscribe({
        next: (res) => {
          this.successMessage = res.message || 'Password updated successfully.';
          setTimeout(() => this.router.navigate(['/inventory']), 2000);
        },
        error: (err) => {
          this.errorMessage = err.error?.message || 'Failed to update password.';
        },
      });
      return;
    }

    if (!this.token) return;

    this.authService.resetPassword(this.token, this.resetForm.value.password).subscribe({
      next: (res) => {
        this.successMessage = res.message;
        setTimeout(() => this.router.navigate(['/login']), 3000);
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Token expired or invalid.';
      },
    });
  }
}
