import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';

export const roleGuard: CanActivateFn = (route, state) => {
  const router = inject(Router);
  const allowedRoles = (route.data?.['roles'] as string[] | undefined) ?? [];

  if (allowedRoles.length === 0) {
    return true;
  }

  const storedUserProfile = localStorage.getItem('user_profile');
  const parsedUserProfile = storedUserProfile ? JSON.parse(storedUserProfile) : null;
  const currentRole = localStorage.getItem('userRole') || parsedUserProfile?.role || parsedUserProfile?.userRole;

  if (currentRole && allowedRoles.includes(currentRole)) {
    return true;
  }

  router.navigate(['/inventory']);
  return false;
};
