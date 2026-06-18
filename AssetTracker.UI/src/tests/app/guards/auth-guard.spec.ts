import { TestBed } from '@angular/core/testing';
import { CanActivateFn, Router } from '@angular/router';
import { of } from 'rxjs';
import { AuthService } from '../../../app/services/auth';
import { authGuard } from '../../../app/guards/auth-guard';

describe('authGuard', () => {
  let router: any;
  const executeGuard: CanActivateFn = (...guardParameters) =>
    TestBed.runInInjectionContext(() => authGuard(...guardParameters));

  beforeEach(() => {
    router = { navigate: vi.fn() };
    TestBed.configureTestingModule({
      providers: [
        {
          provide: Router,
          useValue: router,
        },
        {
          provide: AuthService,
          useValue: {
            isLoggedIn: () => true,
          },
        },
      ],
    });
    localStorage.clear();
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('should be created', () => {
    expect(executeGuard).toBeTruthy();
  });

  it('allows access when user is logged in', () => {
    const result = executeGuard({} as any, {} as any);
    expect(result).toBe(true);
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('navigates to login and denies access when not logged in', () => {
    const authService = TestBed.inject(AuthService) as any;
    authService.isLoggedIn = () => false;

    const result = executeGuard({} as any, {} as any);
    expect(result).toBe(false);
    expect(router.navigate).toHaveBeenCalledWith(['/login']);
  });
});
