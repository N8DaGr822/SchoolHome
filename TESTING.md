# Testing Guide for Homeschool Manager

## Overview
This document outlines the testing strategy and procedures for the Homeschool Manager application.

## Testing Scripts

### 1. Full Test Suite (`test.ps1`)
**When to use:** Before major commits or releases
**What it does:**
- Cleans and restores packages
- Builds the entire solution
- Runs all unit tests
- Checks for warnings
- Starts the application for manual testing
- Provides interactive testing workflow

**Usage:**
```powershell
.\test.ps1
```

### 2. Quick Test (`quick-test.ps1`)
**When to use:** During development for fast feedback
**What it does:**
- Quick build check
- Fast test run
- Minimal output for speed

**Usage:**
```powershell
.\quick-test.ps1
```

### 3. Pre-Push Validation (`pre-push.ps1`)
**When to use:** Before pushing changes to repository
**What it does:**
- Checks for uncommitted changes
- Validates build
- Runs tests
- Checks for critical warnings

**Usage:**
```powershell
.\pre-push.ps1
```

## Manual Testing Checklist

### Core Functionality Tests

#### Teaching Page (`/teach`)
- [ ] Teaching session tracker starts/stops correctly
- [ ] Progress modal displays statistics
- [ ] Report generation modal works
- [ ] Navigation to assignments page works
- [ ] Session data is properly tracked

#### Students Page (`/students`)
- [ ] Student list displays correctly
- [ ] Add new student modal works
- [ ] Edit student modal updates data
- [ ] Student details show correctly
- [ ] Search and filter functionality works

#### Courses Page (`/courses`)
- [ ] Course list displays correctly
- [ ] Add new course modal works
- [ ] Edit course modal updates data
- [ ] Lesson plan management works
- [ ] View/edit lesson plans function properly
- [ ] Course filtering and sorting works

#### Assignments Page (`/assignments`)
- [ ] Assignment list displays correctly
- [ ] Add new assignment modal works
- [ ] Edit assignment modal updates data
- [ ] Grade assignment modal works
- [ ] View assignment details works
- [ ] Search and filter functionality works

#### Resources Page (`/resources`)
- [ ] Grade calculator modal works
- [ ] Lesson timer starts/stops correctly
- [ ] Add resource modal works
- [ ] Navigation to other pages works
- [ ] External resource links are accessible

### UI/UX Tests
- [ ] All modals open and close properly
- [ ] Form validation works correctly
- [ ] Responsive design works on different screen sizes
- [ ] Navigation between pages is smooth
- [ ] Loading states are appropriate
- [ ] Error handling is user-friendly

### Data Integrity Tests
- [ ] Data persists correctly across page navigation
- [ ] Form submissions work without data loss
- [ ] Search and filter results are accurate
- [ ] Modal data is properly synchronized

## Automated Testing

### Unit Tests
Run unit tests with:
```powershell
dotnet test
```

### Build Validation
Check build status with:
```powershell
dotnet build
```

### Code Quality Checks
Check for warnings with:
```powershell
dotnet build --verbosity normal
```

## Testing Workflow

### Development Workflow
1. Make changes to code
2. Run `quick-test.ps1` for immediate feedback
3. Fix any issues found
4. Test manually in browser
5. Commit changes

### Pre-Release Workflow
1. Run `test.ps1` for comprehensive testing
2. Complete manual testing checklist
3. Fix any issues found
4. Run `pre-push.ps1` for final validation
5. Push changes

### Continuous Integration
- All tests must pass before merging
- Build must succeed without errors
- Code quality checks should pass
- Manual testing should be completed

## Common Issues and Solutions

### Build Errors
- **Character literal errors:** Use `&quot;` instead of `'` in Razor syntax
- **Null reference warnings:** Add null checks with `?.` operator
- **Missing dependencies:** Run `dotnet restore`

### Test Failures
- **Test data issues:** Check sample data initialization
- **Timing issues:** Add appropriate delays for async operations
- **Environment issues:** Ensure test environment is properly configured

### UI Issues
- **Modal not showing:** Check modal state variables
- **Form not submitting:** Verify form validation
- **Navigation issues:** Check route configurations

## Performance Testing

### Load Testing
- Test with multiple students/courses/assignments
- Verify search performance with large datasets
- Check modal responsiveness with complex data

### Memory Testing
- Monitor memory usage during extended use
- Check for memory leaks in modals
- Verify proper cleanup of resources

## Security Testing

### Input Validation
- Test form inputs with malicious data
- Verify XSS protection
- Check SQL injection prevention

### Access Control
- Verify proper authorization checks
- Test data isolation between users
- Check for information disclosure

## Browser Compatibility

### Supported Browsers
- Chrome (latest)
- Firefox (latest)
- Safari (latest)
- Edge (latest)

### Testing Checklist
- [ ] All features work in Chrome
- [ ] All features work in Firefox
- [ ] All features work in Safari
- [ ] All features work in Edge
- [ ] Responsive design works on mobile

## Reporting Issues

When reporting testing issues, include:
1. **Environment details:** OS, browser, .NET version
2. **Steps to reproduce:** Detailed step-by-step instructions
3. **Expected behavior:** What should happen
4. **Actual behavior:** What actually happens
5. **Error messages:** Any console errors or build errors
6. **Screenshots:** Visual evidence of the issue

## Continuous Improvement

- Regularly update test scripts
- Add new test cases as features are added
- Improve test coverage over time
- Automate manual testing where possible
- Document new testing procedures
