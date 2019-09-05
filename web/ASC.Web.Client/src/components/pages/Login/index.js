import React, { useState, useEffect, useCallback } from 'react';
import PropTypes from 'prop-types';
import { withRouter } from "react-router";
import { Collapse, Container, Row, Col, Card, CardTitle, CardImg } from 'reactstrap';
import { Button, TextInput, PageLayout } from 'asc-web-components';
import { connect } from 'react-redux';
import { login } from '../../../store/auth/actions';
import styled from 'styled-components';
import { useTranslation } from 'react-i18next';
import i18n from './i18n';
import { welcomePageTitle } from './../../../helpers/customNames';

const FormContainer = styled(Container)`
    margin-top: 70px;

    .login-row {
        margin: 23px 0 0;

        .login-card {
            border: none;

            .card-img {
                max-width: 216px; 
                max-height: 35px;
            }
            .card-title {
                word-wrap: break-word; 
                margin: 8px 0; 
                text-align: left;
                font-size: 24px; 
                color: #116d9d;
            }
        }
    }
`;

const mdOptions = { size: 6, offset: 3 };

const Form = props => {
    const { t } = useTranslation('translation', { i18n });
    const [identifier, setIdentifier] = useState('');
    const [identifierValid, setIdentifierValid] = useState(true);
    const [password, setPassword] = useState('');
    const [passwordValid, setPasswordValid] = useState(true);
    const [errorText, setErrorText] = useState("");
    const [isLoading, setIsLoading] = useState(false);
    const { login, match, location, history } = props;

    const onSubmit = useCallback((e) => {
        //e.preventDefault();

        errorText && setErrorText("");

        let hasError = false;

        if (!identifier.trim()) {
            hasError = true;
            setIdentifierValid(!hasError);
        }

        if (!password.trim()) {
            hasError = true;
            setPasswordValid(!hasError);
        }

        if (hasError)
            return false;

        setIsLoading(true);

        let payload = {
            userName: identifier,
            password: password
        };

        login(payload)
            .then(function () {
                console.log("auth success", match, location, history);
                setIsLoading(false)
                history.push('/');
            })
            .catch(e => {
                console.error("auth error", e);
                setErrorText(e.message);
                setIsLoading(false)
            });
    }, [errorText, history, identifier, location, login, match, password]);

    const onKeyPress = useCallback((target) => {
        if (target.code === "Enter") {
            onSubmit();
        }
    }, [onSubmit]);

    useEffect(() => {
        window.addEventListener('keydown', onKeyPress);
        window.addEventListener('keyup', onKeyPress);
        // Remove event listeners on cleanup
        return () => {
            window.removeEventListener('keydown', onKeyPress);
            window.removeEventListener('keyup', onKeyPress);
        };
    }, [onKeyPress]);

    return (
        <FormContainer>
            <Row className="login-row">
                <Col sm="12" md={mdOptions}>
                    <Card className="login-card">
                        <CardImg className="card-img" src="images/dark_general.png" alt="Logo" top />
                        <CardTitle className="card-title">{t('CustomWelcomePageTitle', { welcomePageTitle })}</CardTitle>
                    </Card>
                </Col>
            </Row>
            <Row className="login-row">
                <Col sm="12" md={mdOptions}>
                    <TextInput
                        id="login"
                        name="login"
                        hasError={!identifierValid}
                        value={identifier}
                        placeholder={t('RegistrationEmailWatermark')}
                        size='huge'
                        scale={true}
                        isAutoFocussed={true}
                        tabIndex={1}
                        isDisabled={isLoading}
                        autocomple="username"
                        onChange={event => {
                            setIdentifier(event.target.value);
                            !identifierValid && setIdentifierValid(true);
                            errorText && setErrorText("");
                        }}
                        onKeyDown={event => onKeyPress(event.target)} />
                </Col>
            </Row>
            <Row className="login-row">
                <Col sm="12" md={mdOptions}>
                    <TextInput
                        id="password"
                        name="password"
                        type="password"
                        hasError={!passwordValid}
                        value={password}
                        placeholder={t('Password')}
                        size='huge'
                        scale={true}
                        tabIndex={2}
                        isDisabled={isLoading}
                        autocomple="current-password"
                        onChange={event => {
                            setPassword(event.target.value);
                            !passwordValid && setPasswordValid(true);
                            errorText && setErrorText("");
                            onKeyPress(event.target);
                        }}
                        onKeyDown={event => onKeyPress(event.target)} />
                </Col>
            </Row>
            <Row className="login-row">
                <Col sm="12" md={mdOptions}>
                    <Button
                        primary
                        size='big'
                        label={isLoading ? t('LoadingProcessing') : t('LoginButton')}
                        tabIndex={3}
                        isDisabled={isLoading}
                        isLoading={isLoading}
                        onClick={onSubmit} />
                </Col>
            </Row>
            <Collapse isOpen={!!errorText}>
                <Row className="login-row">
                    <Col sm="12" md={mdOptions}>
                        <div className="alert alert-danger">{errorText}</div>
                    </Col>
                </Row>
            </Collapse>
        </FormContainer>
    );
}

const LoginForm = (props) => (<PageLayout sectionBodyContent={<Form {...props} />} />);

LoginForm.propTypes = {
    login: PropTypes.func.isRequired,
    match: PropTypes.object.isRequired,
    location: PropTypes.object.isRequired,
    history: PropTypes.object.isRequired
}

LoginForm.defaultProps = {
    identifier: "",
    password: ""
}

export default connect(null, { login })(withRouter(LoginForm));