import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { Router } from '@angular/router';
import { UserService } from '../../app/services/user';
import { App } from '../../app/app';

describe('App', () => {
  let userService: any;
  let router: any;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        {
          provide: UserService,
          useValue: {
            getAllUsers: () => of([]),
          },
        },
        {
          provide: Router,
          useValue: {
            url: '/inventory',
            navigate: () => Promise.resolve(true),
          },
        },
      ],
    }).compileComponents();

    userService = TestBed.inject(UserService);
    router = TestBed.inject(Router);
    localStorage.clear();
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render router outlet shell', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('router-outlet')).toBeTruthy();
  });

  it('isLoggedIn returns true when either token key exists', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;

    expect(app.isLoggedIn()).toBe(false);
    localStorage.setItem('asset_tracker_token', 'jwt');
    expect(app.isLoggedIn()).toBe(true);
  });

  it('getUserRole falls back from localStorage role to parsed profile role', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;

    expect(app.getUserRole()).toBe('Standard User');
    localStorage.setItem('userRole', 'Admin');
    expect(app.getUserRole()).toBe('Admin');

    localStorage.clear();
    localStorage.setItem('user_profile', JSON.stringify({ role: 'Asset Manager' }));
    expect(app.getUserRole()).toBe('Asset Manager');
  });

  it('isAdminOrManager returns true for Admin and Asset Manager roles', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;

    localStorage.setItem('userRole', 'Admin');
    expect(app.isAdminOrManager()).toBe(true);

    localStorage.setItem('userRole', 'Asset Manager');
    expect(app.isAdminOrManager()).toBe(true);

    localStorage.setItem('userRole', 'Standard User');
    expect(app.isAdminOrManager()).toBe(false);
  });

  it('isAdmin returns true only for Admin role', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;

    localStorage.setItem('userRole', 'Admin');
    expect(app.isAdmin()).toBe(true);

    localStorage.setItem('userRole', 'Asset Manager');
    expect(app.isAdmin()).toBe(false);
  });

  it('isAuthRoute identifies login and register paths', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;

    router.url = '/login';
    expect(app.isAuthRoute()).toBe(true);

    router.url = '/register';
    expect(app.isAuthRoute()).toBe(true);

    router.url = '/inventory';
    expect(app.isAuthRoute()).toBe(false);

    router.url = '/login?redirect=true';
    expect(app.isAuthRoute()).toBe(true);
  });

  it('logout clears all auth storage keys', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;

    localStorage.setItem('token', 'a');
    localStorage.setItem('asset_tracker_token', 'b');
    localStorage.setItem('userRole', 'Admin');
    localStorage.setItem('user_profile', '{}');

    app.logout();

    expect(localStorage.getItem('token')).toBeNull();
    expect(localStorage.getItem('asset_tracker_token')).toBeNull();
    expect(localStorage.getItem('userRole')).toBeNull();
    expect(localStorage.getItem('user_profile')).toBeNull();
  });

  it('getUserProfile correctly parses user_profile from localStorage', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;

    expect(app.getUserProfile()).toBeNull();

    localStorage.setItem('user_profile', JSON.stringify({ fullName: 'Alice Smith', email: 'alice@company.com', role: 'Asset Manager' }));
    expect(app.getUserProfile()).toEqual({
      fullName: 'Alice Smith',
      email: 'alice@company.com',
      role: 'Asset Manager'
    });
  });

  it('toggleUserInfo toggles showUserInfo state', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;

    expect(app.showUserInfo).toBe(false);
    
    const mockEvent = { stopPropagation: vi.fn() } as any;
    app.toggleUserInfo(mockEvent);
    expect(app.showUserInfo).toBe(true);
    expect(mockEvent.stopPropagation).toHaveBeenCalled();

    app.toggleUserInfo(mockEvent);
    expect(app.showUserInfo).toBe(false);
  });

  it('onDocumentClick sets showUserInfo to false', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;

    app.showUserInfo = true;
    app.onDocumentClick();
    expect(app.showUserInfo).toBe(false);
  });
});




